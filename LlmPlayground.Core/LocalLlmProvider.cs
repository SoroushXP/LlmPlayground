using System.Diagnostics;
using System.Runtime.CompilerServices;
using LLama;
using LLama.Common;
using LLama.Native;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlmPlayground.Core;

/// <summary>
/// Specifies the backend to use for LLM inference.
/// </summary>
public enum LlmBackendType
{
    /// <summary>
    /// CPU-only inference.
    /// </summary>
    Cpu,

    /// <summary>
    /// Vulkan GPU acceleration (AMD, Intel, NVIDIA).
    /// </summary>
    Vulkan,

    /// <summary>
    /// CUDA GPU acceleration (NVIDIA only).
    /// </summary>
    Cuda
}

/// <summary>
/// Configuration for the local LLM provider.
/// </summary>
public record LocalLlmConfiguration
{
    /// <summary>
    /// Path to the GGUF model file.
    /// </summary>
    public required string ModelPath { get; init; }

    /// <summary>
    /// Backend type for inference (Cpu, Vulkan, Cuda).
    /// </summary>
    public LlmBackendType Backend { get; init; } = LlmBackendType.Cpu;

    /// <summary>
    /// GPU device index to use (for multi-GPU systems).
    /// </summary>
    public int GpuDeviceIndex { get; init; } = 0;

    /// <summary>
    /// Number of GPU layers to offload (0 = CPU only, -1 = all layers).
    /// </summary>
    public int GpuLayerCount { get; init; } = 0;

    /// <summary>
    /// Context size in tokens.
    /// </summary>
    public uint ContextSize { get; init; } = 2048;

    /// <summary>
    /// Number of threads to use for inference.
    /// </summary>
    public uint ThreadCount { get; init; } = (uint)Math.Max(1, Environment.ProcessorCount / 2);
}

/// <summary>
/// LLM provider that uses a locally downloaded GGUF model file via LLamaSharp.
/// Supports models like Llama, Mistral, Phi, and other llama.cpp compatible models.
/// </summary>
public sealed class LocalLlmProvider : ILlmProvider
{
    private readonly LocalLlmConfiguration _configuration;
    private readonly ILogger _logger;
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalLlmProvider"/> class.
    /// </summary>
    /// <param name="configuration">Local LLM configuration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public LocalLlmProvider(LocalLlmConfiguration configuration, ILogger<LocalLlmProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration.ModelPath))
            throw new ArgumentException("Model path cannot be empty.", nameof(configuration));

        if (!File.Exists(configuration.ModelPath))
            throw new FileNotFoundException($"Model file not found: {configuration.ModelPath}");

        _configuration = configuration;
        _logger = logger ?? NullLogger<LocalLlmProvider>.Instance;
    }

    /// <inheritdoc />
    public string ProviderName => $"LocalLlm (LLamaSharp - {_configuration.Backend})";

    /// <inheritdoc />
    public bool IsReady => _executor != null && !_disposed;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_executor != null)
            return Task.CompletedTask;

        _logger.LogInformation("Initializing LocalLlm with backend {Backend}, model {ModelPath}",
            _configuration.Backend, _configuration.ModelPath);

        // Configure the native library based on backend type
        ConfigureBackend();

        var parameters = new ModelParams(_configuration.ModelPath)
        {
            ContextSize = _configuration.ContextSize,
            GpuLayerCount = _configuration.GpuLayerCount,
            Threads = (int)_configuration.ThreadCount,
            MainGpu = _configuration.GpuDeviceIndex
        };

        _logger.LogDebug("Loading model with ContextSize={ContextSize}, GpuLayers={GpuLayers}, Threads={Threads}",
            _configuration.ContextSize, _configuration.GpuLayerCount, _configuration.ThreadCount);

        _model = LLamaWeights.LoadFromFile(parameters);
        _context = _model.CreateContext(parameters);
        _executor = new InteractiveExecutor(_context);

        _logger.LogInformation("LocalLlm initialized successfully");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures the native library backend based on configuration.
    /// </summary>
    private void ConfigureBackend()
    {
        // LLamaSharp auto-detects available backends, but we can influence loading order
        // by ensuring the correct native library is loaded first
        switch (_configuration.Backend)
        {
            case LlmBackendType.Vulkan:
                // Vulkan backend will be used if available (works with AMD, Intel, NVIDIA)
                // The LLamaSharp.Backend.Vulkan package provides the native libraries
                _logger.LogDebug("Configuring Vulkan backend");
                NativeLibraryConfig.All.WithAutoFallback(false);
                break;

            case LlmBackendType.Cuda:
                // CUDA backend for NVIDIA GPUs
                // Requires LLamaSharp.Backend.Cuda package (not included by default)
                _logger.LogDebug("Configuring CUDA backend");
                NativeLibraryConfig.All.WithAutoFallback(false);
                NativeLibraryConfig.All.WithCuda(true);
                break;

            case LlmBackendType.Cpu:
            default:
                // CPU-only backend
                _logger.LogDebug("Configuring CPU backend");
                NativeLibraryConfig.All.WithAutoFallback(true);
                break;
        }
    }

    /// <inheritdoc />
    public async Task<LlmCompletionResult> CompleteAsync(
        string prompt,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        options ??= new LlmInferenceOptions();
        var inferenceParams = CreateInferenceParams(options);

        _logger.LogDebug("Starting completion, MaxTokens={MaxTokens}", options.MaxTokens);

        var stopwatch = Stopwatch.StartNew();
        var tokens = new List<string>();

        await foreach (var token in _executor!.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            tokens.Add(token);
        }

        stopwatch.Stop();

        var fullText = string.Join("", tokens);

        _logger.LogDebug("Completed in {Duration}ms, {Tokens} tokens",
            stopwatch.ElapsedMilliseconds, tokens.Count);

        return new LlmCompletionResult(fullText, tokens.Count, stopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        LlmInferenceOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        options ??= new LlmInferenceOptions();
        var inferenceParams = CreateInferenceParams(options);

        _logger.LogDebug("Starting streaming completion");

        await foreach (var token in _executor!.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            yield return token;
        }
    }

    /// <inheritdoc />
    public async Task<LlmCompletionResult> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        LlmInferenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // For LocalLlm, format messages into a single prompt
        var prompt = FormatMessagesAsPrompt(messages);

        _logger.LogDebug("Chat with {MessageCount} messages", messages.Count);

        return await CompleteAsync(prompt, options, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ChatStreamingAsync(
        IReadOnlyList<ChatMessage> messages,
        LlmInferenceOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For LocalLlm, format messages into a single prompt
        var prompt = FormatMessagesAsPrompt(messages);

        _logger.LogDebug("Streaming chat with {MessageCount} messages", messages.Count);

        await foreach (var token in CompleteStreamingAsync(prompt, options, cancellationToken))
        {
            yield return token;
        }
    }

    private static string FormatMessagesAsPrompt(IReadOnlyList<ChatMessage> messages)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var msg in messages)
        {
            var role = msg.Role switch
            {
                ChatRole.System => "System",
                ChatRole.User => "User",
                ChatRole.Assistant => "Assistant",
                _ => "User"
            };
            sb.AppendLine($"{role}: {msg.Content}");
        }
        sb.AppendLine("Assistant:");
        return sb.ToString();
    }

    private static InferenceParams CreateInferenceParams(LlmInferenceOptions options)
    {
        return new InferenceParams
        {
            MaxTokens = options.MaxTokens,
            SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline
            {
                Temperature = options.Temperature,
                TopP = options.TopP,
                RepeatPenalty = options.RepeatPenalty
            },
            AntiPrompts = ["User:", "\n\n"]
        };
    }

    private void EnsureInitialized()
    {
        if (_executor == null)
            throw new InvalidOperationException(
                "Provider is not initialized. Call InitializeAsync() first.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogDebug("Disposing LocalLlm provider");

        _executor = null;
        _context?.Dispose();
        _model?.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

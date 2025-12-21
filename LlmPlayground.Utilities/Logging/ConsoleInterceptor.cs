using System.Text;

namespace LlmPlayground.Utilities.Logging;

/// <summary>
/// Intercepts console output and routes it through the logging system.
/// </summary>
public sealed class ConsoleInterceptor : IDisposable
{
    private readonly LoggerService _logger;
    private readonly object _lock = new();
    
    private TextWriter? _originalOut;
    private TextWriter? _originalError;
    private InterceptingTextWriter? _interceptedOut;
    private InterceptingTextWriter? _interceptedError;
    
    /// <summary>
    /// Gets a value indicating whether the interceptor is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Initializes a new console interceptor.
    /// </summary>
    public ConsoleInterceptor(LoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts intercepting console output.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (IsActive) return;

            _originalOut = Console.Out;
            _originalError = Console.Error;

            _interceptedOut = new InterceptingTextWriter(_originalOut, text => _logger.LogConsoleCapture(text, isError: false));
            _interceptedError = new InterceptingTextWriter(_originalError, text => _logger.LogConsoleCapture(text, isError: true));

            Console.SetOut(_interceptedOut);
            Console.SetError(_interceptedError);

            IsActive = true;
        }
    }

    /// <summary>
    /// Stops intercepting console output and restores original streams.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!IsActive) return;

            if (_originalOut is not null)
                Console.SetOut(_originalOut);
            
            if (_originalError is not null)
                Console.SetError(_originalError);

            _interceptedOut?.Dispose();
            _interceptedError?.Dispose();

            _interceptedOut = null;
            _interceptedError = null;
            _originalOut = null;
            _originalError = null;

            IsActive = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// TextWriter wrapper that intercepts writes and passes them to a callback while also writing to the original stream.
    /// </summary>
    private sealed class InterceptingTextWriter : TextWriter
    {
        private readonly TextWriter _inner;
        private readonly Action<string> _onWrite;
        private readonly StringBuilder _lineBuffer = new();
        private readonly object _bufferLock = new();

        public override Encoding Encoding => _inner.Encoding;

        public InterceptingTextWriter(TextWriter inner, Action<string> onWrite)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _onWrite = onWrite ?? throw new ArgumentNullException(nameof(onWrite));
        }

        public override void Write(char value)
        {
            _inner.Write(value);
            
            lock (_bufferLock)
            {
                if (value == '\n')
                {
                    FlushLineBuffer();
                }
                else if (value != '\r')
                {
                    _lineBuffer.Append(value);
                }
            }
        }

        public override void Write(string? value)
        {
            if (value is null) return;
            
            _inner.Write(value);
            ProcessText(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _inner.Write(buffer, index, count);
            ProcessText(new string(buffer, index, count));
        }

        public override void WriteLine()
        {
            _inner.WriteLine();
            FlushLineBuffer();
        }

        public override void WriteLine(string? value)
        {
            _inner.WriteLine(value);
            
            lock (_bufferLock)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _lineBuffer.Append(value);
                }
                FlushLineBuffer();
            }
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            _inner.WriteLine(buffer, index, count);
            
            lock (_bufferLock)
            {
                _lineBuffer.Append(buffer, index, count);
                FlushLineBuffer();
            }
        }

        public override Task WriteAsync(char value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(string? value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync()
        {
            WriteLine();
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char value)
        {
            WriteLine(value.ToString());
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(string? value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            WriteLine(buffer, index, count);
            return Task.CompletedTask;
        }

        public override void Flush()
        {
            _inner.Flush();
            FlushLineBuffer();
        }

        public override Task FlushAsync()
        {
            Flush();
            return Task.CompletedTask;
        }

        private void ProcessText(string text)
        {
            lock (_bufferLock)
            {
                foreach (var ch in text)
                {
                    if (ch == '\n')
                    {
                        FlushLineBuffer();
                    }
                    else if (ch != '\r')
                    {
                        _lineBuffer.Append(ch);
                    }
                }
            }
        }

        private void FlushLineBuffer()
        {
            if (_lineBuffer.Length > 0)
            {
                try
                {
                    _onWrite(_lineBuffer.ToString());
                }
                catch
                {
                    // Swallow logging errors to prevent infinite loops
                }
                _lineBuffer.Clear();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FlushLineBuffer();
            }
            base.Dispose(disposing);
        }
    }
}


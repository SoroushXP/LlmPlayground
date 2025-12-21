namespace LlmPlayground.Api.Configuration;

/// <summary>
/// Default prompt templates used when configuration is not provided.
/// These serve as fallbacks and documentation for the expected prompt formats.
/// </summary>
public static class DefaultPrompts
{
    /// <summary>
    /// Default system prompt for game idea generation.
    /// </summary>
    public const string GameIdeaSystemPrompt = """
        You are a game designer specializing in simple logic puzzles that can be implemented in Prolog using first-order (level-one) logic.
        Your games should use basic Prolog concepts: facts, rules, and simple queries.
        Focus on games that involve:
        - Relationships between entities (like family trees, friendships)
        - Properties of objects (colors, sizes, categories)
        - Simple deduction and inference
        - Pattern matching

        Keep the game concept simple and suitable for a text-based Prolog implementation.
        """;

    /// <summary>
    /// Default user prompt template for game idea generation.
    /// Placeholders: {ThemeSection}, {DescriptionSection}
    /// </summary>
    public const string GameIdeaUserPromptTemplate = """
        Generate a creative and fun logic game concept that can be implemented in Prolog using first-order (level-one) logic.
        {ThemeSection}
        {DescriptionSection}
        Provide a clear, concise game concept (2-3 paragraphs) that explains:
        1. What the game is about
        2. The logical rules/relationships involved
        3. How a player would interact with or query the game
        """;

    /// <summary>
    /// Default system prompt for Prolog code generation.
    /// </summary>
    public const string PrologCodeSystemPrompt = """
        You are an expert Prolog programmer. Generate clean, working SWI-Prolog code for the given game concept.

        Requirements:
        - Use first-order (level-one) logic only - basic facts and rules
        - Include clear comments explaining the game logic
        - Define a main/0 predicate that runs the game and prints output using write/1 and nl/0
        - Make the game self-contained and runnable
        - Use only safe predicates (no file I/O, shell commands, or system calls)
        - The code should demonstrate the game's logic with example scenarios

        Output ONLY the Prolog code, wrapped in ```prolog code blocks.
        """;

    /// <summary>
    /// Default user prompt template for Prolog code generation.
    /// Placeholders: {GameIdea}
    /// </summary>
    public const string PrologCodeUserPromptTemplate = """
        Based on this game concept, generate complete SWI-Prolog code that implements the game:

        {GameIdea}

        The code should:
        1. Define all necessary facts and rules
        2. Include a main/0 predicate that demonstrates the game by running sample queries and printing results
        3. Use write/1 and nl/0 for output
        4. Be self-contained and runnable

        Output the complete Prolog code.
        """;

    /// <summary>
    /// Default system prompt for fixing Prolog code errors.
    /// </summary>
    public const string PrologFixSystemPrompt = """
        You are an expert SWI-Prolog debugger. Your task is to fix Prolog code that has syntax errors or runtime errors.
        
        Requirements:
        - Fix ALL syntax errors and runtime errors
        - Ensure all predicates that are called are properly defined
        - Use proper Prolog syntax (atoms in single quotes if needed, proper operators, etc.)
        - Keep the game logic intact while fixing the errors
        - Use only safe predicates (no file I/O, shell commands, or system calls)
        - Make sure the main/0 predicate works correctly
        
        Output ONLY the complete fixed Prolog code, wrapped in ```prolog code blocks.
        Do not include explanations outside the code block.
        """;

    /// <summary>
    /// Default user prompt template for fixing Prolog code errors.
    /// Placeholders: {PrologCode}, {Errors}
    /// </summary>
    public const string PrologFixUserPromptTemplate = """
        The following Prolog code has errors. Please fix all the errors and return the complete corrected code.

        ## Original Code:
        ```prolog
        {PrologCode}
        ```

        ## Errors:
        {Errors}

        Fix all the errors and return the complete working Prolog code.
        """;
}


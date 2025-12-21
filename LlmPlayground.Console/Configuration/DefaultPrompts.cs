namespace LlmPlayground.Console.Configuration;

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
        - Use only safe predicates (no file I/O, shell commands, or system calls)
        - Prefix singleton variables with underscore (e.g., _Unused) to avoid warnings

        CRITICAL - The main/0 predicate MUST:
        1. Print a title/header for the game
        2. Display the initial facts and setup
        3. Run AT LEAST 3-5 example queries that demonstrate the game logic
        4. Show the query being asked and its result (e.g., "Query: Who is the parent of X? Answer: john")
        5. Demonstrate deduction/inference by showing how rules derive new facts
        6. Produce at least 10-15 lines of meaningful output

        The main/0 predicate should NOT just print a simple message and exit.
        It must actively demonstrate the game by running findall/3, member/2, or direct queries and printing results.

        Output ONLY the Prolog code, wrapped in ```prolog code blocks.
        """;

    /// <summary>
    /// Default user prompt template for Prolog code generation.
    /// Placeholders: {GameIdea}
    /// </summary>
    public const string PrologCodeUserPromptTemplate = """
        Based on this game concept, generate complete SWI-Prolog code that implements the game:

        {GameIdea}

        The code MUST:
        1. Define all necessary facts and rules with proper Prolog syntax
        2. Prefix singleton variables with underscore to avoid warnings
        3. Include a main/0 predicate that ACTIVELY DEMONSTRATES the game:
           - Print a game title/header
           - Show the game setup (facts, entities, relationships)
           - Run 3-5 example queries using the rules you defined
           - Display each query and its results clearly
           - Show logical deductions or inferences
           - Produce at least 10-15 lines of output
        4. Use write/1, writeln/1, format/2, and nl/0 for output
        5. Use findall/3 or forall/2 to collect and display query results

        DO NOT just print a completion message. The main/0 must run actual queries and show results.

        Output the complete Prolog code.
        """;

    /// <summary>
    /// Default system prompt for fixing Prolog code errors.
    /// </summary>
    public const string PrologFixSystemPrompt = """
        You are an expert SWI-Prolog debugger. Your task is to fix Prolog code that has errors or produces insufficient output.

        Requirements:
        - Fix ALL syntax errors and runtime errors
        - Ensure all predicates that are called are properly defined
        - Use proper Prolog syntax (atoms in single quotes if needed, proper operators, etc.)
        - Prefix singleton variables with underscore (e.g., _Unused) to avoid warnings
        - Keep the game logic intact while fixing the errors
        - Use only safe predicates (no file I/O, shell commands, or system calls)

        CRITICAL - The main/0 predicate MUST:
        - Run actual queries and display their results (not just print static messages)
        - Produce at least 10-15 lines of meaningful output
        - Show query results using findall/3, forall/2, or direct predicate calls
        - Demonstrate the game logic by showing relationships, deductions, and answers

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


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
        You are a game designer specializing in SOLVABLE logic puzzles (like Zebra puzzles, Einstein riddles, or mystery games).

        Your puzzles MUST have:
        1. A HIDDEN SECRET the player must discover (e.g., "Who stole the diamond?", "Which box has the prize?")
        2. A set of CLUES that, when combined through logical deduction, reveal the secret
        3. EXACTLY ONE correct solution that can be deduced from the clues

        CRITICAL: Your clues must be LOGICALLY CONSISTENT!
        - If clue A says "The thief was in the library", no other clue can say "No one was in the library"
        - If the answer is "Bob", the clues must logically lead to Bob, not contradict it
        - Test your puzzle mentally: can you solve it step by step from the clues?

        Good puzzle types:
        - "Whodunit" mysteries: Deduce who committed a crime from alibis and clues
        - Zebra/Einstein puzzles: Figure out who owns what, lives where, etc.
        - Logic grid puzzles: Match items across categories using elimination
        - Treasure hunts: Deduce which location contains the treasure

        The puzzle must be SOLVABLE through pure logical deduction - no guessing!
        """;

    /// <summary>
    /// Default user prompt template for game idea generation.
    /// Placeholders: {ThemeSection}, {DescriptionSection}
    /// </summary>
    public const string GameIdeaUserPromptTemplate = """
        Design a SOLVABLE logic puzzle game for Prolog.
        {ThemeSection}
        {DescriptionSection}

        Your puzzle concept MUST include:
        1. THE SECRET: What is the hidden answer the player must discover? (Be specific!)
        2. THE SETUP: What entities exist? (3-5 suspects, locations, items, etc.)
        3. THE CLUES: List 4-6 specific clues that logically lead to the solution
           - Each clue should eliminate possibilities or establish relationships
           - Together, the clues must uniquely determine the answer
        4. THE SOLUTION: State the correct answer (this will be hidden from the player)
        5. HOW TO PLAY: What query should the player run to check their answer?

        Example format:
        - Secret: "Which suspect stole the ruby?"
        - Setup: 3 suspects (Alice, Bob, Carol), 3 rooms, 3 times
        - Clues: "The thief was in the library", "Alice was in the kitchen", etc.
        - Solution: "Bob stole the ruby"
        - Query: "thief(X)" to find the answer
        """;

    /// <summary>
    /// Default system prompt for Prolog code generation.
    /// </summary>
    public const string PrologCodeSystemPrompt = """
        You are an expert Prolog programmer creating PLAYABLE logic puzzle games.

        Requirements:
        - Use first-order logic with facts and rules
        - Use only safe predicates (no file I/O, shell commands, or system calls)
        - Prefix singleton variables with underscore (e.g., _Unused)

        GAME STRUCTURE - You MUST implement:
        1. Facts that encode the puzzle constraints (clues as Prolog facts/rules)
        2. A "solve/1" predicate (ONE argument only!) that DEDUCES the answer through logical reasoning
        3. A "check_answer/1" predicate so players can verify their guess
        4. A "hint/1" predicate that gives progressive hints

        CRITICAL - solve/1 MUST:
        - Have exactly ONE argument: solve(X) where X is the culprit/answer
        - DO NOT use solve/2 like solve(X, Y) - that is WRONG!
        - DO NOT just hardcode the answer like: solve(bob).
        - MUST deduce the answer from the clues using Prolog rules
        - Example: solve(X) :- suspect(X), has_motive(X), was_at_scene(X), \+ has_alibi(X).
        - The clues must be encoded as facts that the solve/1 rule queries

        *** CRITICAL PROLOG LOGIC ERROR TO AVOID ***
        If your solve/1 rule queries a predicate like at(X, gallery, morning), you MUST have
        facts defined that can MATCH that query! Example of BROKEN code:

            at(alice, kitchen, noon).        % Only defines alice
            solve(X) :- at(X, gallery, morning).  % FAILS! No one is at gallery in morning!

        CORRECT approach - define facts for ALL possibilities or use different logic:

            Option A: Define the fact that makes solve/1 work:
                at(bob, gallery, morning).   % Bob was at gallery in morning
                solve(X) :- at(X, gallery, morning).  % Returns bob

            Option B: Use negation to eliminate suspects:
                suspect(alice). suspect(bob). suspect(carol).
                alibi(alice).   % Alice has alibi
                alibi(carol).   % Carol has alibi
                solve(X) :- suspect(X), \+ alibi(X).  % Returns bob (no alibi)

        BEFORE writing solve/1, list ALL the predicates it will query and verify you have
        defined facts that will make at least one suspect match ALL conditions!

        The main/0 predicate MUST:
        1. Print a welcome message and story setup
        2. List ALL the clues the player can use
        3. Show example queries the player can try
        4. Tell the player: "Type solve(X) to find the answer, or check_answer(your_guess) to verify"
        5. DO NOT reveal the answer in main/0!

        IMPORTANT: After main/0, the code will be tested by running solve(X) to verify it works.
        If solve(X) fails or returns the wrong answer, the code will be rejected.

        Output ONLY the Prolog code, wrapped in ```prolog code blocks.
        """;

    /// <summary>
    /// Default user prompt template for Prolog code generation.
    /// Placeholders: {GameIdea}
    /// </summary>
    public const string PrologCodeUserPromptTemplate = """
        Create a PLAYABLE Prolog logic puzzle based on this concept:

        {GameIdea}

        REQUIRED STRUCTURE:

        1. FACTS section: Define the puzzle world (suspects, locations, items, times, etc.)
           Encode the clues as facts that can be queried.

        2. DEDUCTION RULES: Create rules that encode the logical constraints
           Example: can_be_thief(X) :- suspect(X), was_at(X, crime_scene, Time), crime_time(Time).
           Example: eliminated(X) :- suspect(X), has_alibi(X).

        3. solve/1 predicate - MUST have ONE argument and USE LOGICAL DEDUCTION:
           WRONG (two args): solve(X, Y) :- ...   <-- DO NOT USE solve/2!
           BAD (hardcoded):  solve(bob).
           GOOD (deduction): solve(X) :- can_be_thief(X), \+ eliminated(X).

           The solve/1 predicate must:
           - Have exactly ONE argument (the answer)
           - Deduce the answer by querying facts and rules
           - NOT just return a hardcoded value

        4. PLAYER INTERFACE predicates:
           - solve(X) - Finds THE answer (one argument only!)
           - check_answer(Guess) - Returns true if Guess matches solve(X)
           - show_clues - Displays all clues
           - hint(N) - Shows hint number N

        5. main/0 predicate MUST:
           - Print welcome message and story
           - Call show_clues to display the puzzle clues
           - Print instructions: "Commands: solve(X), check_answer(guess), hint(1), hint(2)..."
           - DO NOT reveal the answer!

        *** MANDATORY VERIFICATION BEFORE SUBMITTING ***

        After writing your code, you MUST verify solve/1 will work:

        Step 1: List every predicate solve/1 calls (e.g., suspect/1, at/3, motive/1)
        Step 2: For each predicate, check: "Do I have facts that match this query?"
        Step 3: Find ONE suspect that satisfies ALL conditions in solve/1

        COMMON FATAL ERROR:
        If solve/1 contains: at(X, gallery, morning)
        You MUST have a fact like: at(bob, gallery, morning).
        If no such fact exists, solve(X) will FAIL and your code is BROKEN!

        CORRECT PATTERN using elimination (recommended):
            suspect(alice). suspect(bob). suspect(carol).
            % Clues eliminate suspects:
            has_alibi(alice).    % "Alice was seen elsewhere"
            has_alibi(carol).    % "Carol was at the party"
            % Bob has no alibi - he's the answer!
            solve(X) :- suspect(X), \+ has_alibi(X).

        WRONG PATTERN (will fail):
            suspect(alice). suspect(bob). suspect(carol).
            % No facts about who was at the crime scene!
            solve(X) :- suspect(X), at(X, crime_scene, midnight).  % FAILS - no at/3 facts!

        Output the complete Prolog code.
        """;

    /// <summary>
    /// Default system prompt for fixing Prolog code errors.
    /// </summary>
    public const string PrologFixSystemPrompt = """
        You are an expert SWI-Prolog debugger fixing a logic puzzle game.

        Requirements:
        - Fix ALL syntax and runtime errors
        - Ensure all predicates are properly defined before use
        - Use proper Prolog syntax (atoms in quotes if needed, correct operators)
        - Prefix singleton variables with underscore (e.g., _Unused)
        - Use only safe predicates (no file I/O, shell commands, system calls)

        The game MUST have these working predicates:
        - solve(X) - ONE argument only! Must find the answer through LOGICAL DEDUCTION
        - check_answer(Guess) - Must verify if guess is correct
        - show_clues - Must display puzzle clues
        - main/0 - Must show story, clues, and instructions (10+ lines output)

        CRITICAL REQUIREMENTS FOR solve/1:
        1. solve/1 must have EXACTLY ONE argument - NOT solve(X, Y)!
        2. solve(X) must use LOGICAL DEDUCTION to find X, not just return a hardcoded answer
        3. The clues in the puzzle must be encoded as facts that solve/1 queries
        4. solve(X) must return exactly ONE answer that is consistent with all clues
        5. If the current solve/1 just returns a hardcoded value, REWRITE it to use deduction

        *** MOST COMMON BUG: solve/1 QUERIES PREDICATES WITH NO MATCHING FACTS ***

        If solve/1 contains a goal like: at(X, gallery, morning)
        But you only have facts like: at(fiona, museum, afternoon). at(diana, library, evening).
        Then solve(X) will FAIL because NO ONE matches at(X, gallery, morning)!

        TO FIX THIS: Either add the missing fact OR rewrite solve/1 to use elimination:

        SOLUTION A - Add the missing fact:
            at(george, gallery, morning).  % Add fact that makes solve/1 work
            solve(X) :- suspect(X), at(X, gallery, morning).  % Now returns george

        SOLUTION B - Use elimination pattern (RECOMMENDED):
            suspect(alice). suspect(bob). suspect(carol).
            has_alibi(alice).  % Alice was elsewhere
            has_alibi(carol).  % Carol was at party
            % Bob has no alibi - he's guilty!
            solve(X) :- suspect(X), \+ has_alibi(X).  % Returns bob

        Example of WRONG solve (two arguments):
          solve(X, Y) :- thief(X), item(Y).  <-- WRONG! Use solve/1!

        Example of BAD solve/1 (hardcoded):
          solve(frank).

        Example of GOOD solve/1 (deduction):
          solve(X) :- suspect(X), was_at_scene(X), has_motive(X), \+ has_alibi(X).

        Output ONLY the complete fixed Prolog code in ```prolog blocks.
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

        *** DEBUGGING CHECKLIST ***

        1. FIND solve/1 in the code above
        2. LIST every predicate that solve/1 calls (e.g., suspect/1, at/3, motive/1)
        3. For EACH predicate, search the code: "Are there facts that match this query?"
        4. If solve/1 has: at(X, gallery, morning)
           Search for facts like: at(someone, gallery, morning).
           If NO such fact exists, that's the bug!

        *** HOW TO FIX ***

        Option A: Add the missing fact that makes one suspect match:
            at(george, gallery, morning).  % George was at the crime scene

        Option B: Rewrite solve/1 to use elimination (more robust):
            % Instead of requiring positive facts, eliminate suspects with alibis
            has_alibi(alice).
            has_alibi(carol).
            solve(X) :- suspect(X), \+ has_alibi(X).  % Returns whoever has no alibi

        IMPORTANT:
        - If the error mentions solve/2, change solve(X, Y) to solve(X) with ONE argument only!
        - If the error mentions hardcoded answer, rewrite solve/1 to deduce from facts
        - solve/1 must have exactly ONE argument and use logical deduction
        - VERIFY: After fixing, trace through solve(X) - will it find at least one answer?

        Fix all the errors and return the complete working Prolog code.
        """;
}


% Arithmetic operations test file

% Calculate factorial
factorial(0, 1) :- !.
factorial(N, F) :-
    N > 0,
    N1 is N - 1,
    factorial(N1, F1),
    F is N * F1.

% Calculate sum of list
sum_list([], 0).
sum_list([H|T], Sum) :-
    sum_list(T, Rest),
    Sum is H + Rest.

% Test predicate that prints results
run_tests :-
    factorial(5, F5),
    format('Factorial of 5 is ~w~n', [F5]),
    sum_list([1, 2, 3, 4, 5], S),
    format('Sum of [1,2,3,4,5] is ~w~n', [S]).




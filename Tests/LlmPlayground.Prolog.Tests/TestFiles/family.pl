% Family relationships test file

% Facts
parent(tom, mary).
parent(tom, john).
parent(mary, ann).
parent(mary, pat).

male(tom).
male(john).
female(mary).
female(ann).
female(pat).

% Rules
father(X, Y) :- parent(X, Y), male(X).
mother(X, Y) :- parent(X, Y), female(X).
grandparent(X, Z) :- parent(X, Y), parent(Y, Z).

% Query helpers
find_fathers :-
    forall(father(F, C), format('~w is father of ~w~n', [F, C])).

find_grandparents :-
    forall(grandparent(GP, GC), format('~w is grandparent of ~w~n', [GP, GC])).




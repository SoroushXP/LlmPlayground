% Family relationships example
% Run with: dotnet run -- Examples/family.pl "demo"

% Facts: parent(Parent, Child)
parent(tom, mary).
parent(tom, john).
parent(mary, ann).
parent(mary, pat).
parent(pat, jim).

% Facts: male/female
male(tom).
male(john).
male(jim).
female(mary).
female(ann).
female(pat).

% Rules
father(X, Y) :- parent(X, Y), male(X).
mother(X, Y) :- parent(X, Y), female(X).

grandparent(X, Z) :- parent(X, Y), parent(Y, Z).

sibling(X, Y) :- 
    parent(P, X), 
    parent(P, Y), 
    X \= Y.

ancestor(X, Y) :- parent(X, Y).
ancestor(X, Y) :- parent(X, Z), ancestor(Z, Y).

% Demo predicate
demo :-
    write('=== Family Relationships Demo ==='), nl, nl,
    
    write('Who is Tom the father of?'), nl,
    forall(father(tom, X), (write('  - '), write(X), nl)),
    nl,
    
    write('Who is Mary the mother of?'), nl,
    forall(mother(mary, X), (write('  - '), write(X), nl)),
    nl,
    
    write('Who are the grandparents and grandchildren?'), nl,
    forall(grandparent(GP, GC), 
           (write('  - '), write(GP), write(' is grandparent of '), write(GC), nl)),
    nl,
    
    write('Who are siblings?'), nl,
    forall((sibling(X, Y), X @< Y), 
           (write('  - '), write(X), write(' and '), write(Y), nl)),
    nl,
    
    write('All ancestors of Jim:'), nl,
    forall(ancestor(A, jim), (write('  - '), write(A), nl)),
    nl.




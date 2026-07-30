# CBI11 declaration succession capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI10 left one direction open: a declared dependency nothing exercised cannot be shown unnecessary,
because absence of use is not evidence of absence of need. CBI11 answers the question that leaves —
what could ever retire an unexercised declaration — and the answer is **not evidence of disuse**. It
is the Component saying so.

A declaration's names come from the requested authority CM2 records for the selected definition. So
a declaration narrows exactly when a **successor resolution** of the same position declares less.
The Component's own re-declaration is the permission; observation's only role is the veto: an
authority the member has already exercised cannot be narrowed away, even when the successor stops
declaring it.

Nothing here retires a member, and no elapsed time, interaction count, or quiet period narrows
anything.

## C1 — narrowing needs a successor resolution of the same position

The caller supplies a completed successor generation. It must resolve the same requirement to the
same definition and occurrence, as one direct `1..1` distinct position, under the same binding scope
the live member records. A different position is a different binding, not a successor.

Property: no generation that resolves a different requirement, definition, occurrence, cardinality,
exposure, or binding scope can narrow this member's declaration.

## C2 — both declarations must be the ones their generations record

The declaration in force is checked against the current generation and the successor declaration
against the successor generation, by the rule CBI9 uses: names equal to the requested authority
recorded for the selected definition, one mapping entry each, distinct tuples. A successor that
declares nothing is refused rather than treated as depending on nothing.

Property: no narrowing proceeds on a declaration either generation does not record.

## C3 — succession only narrows

The successor's names must be a strict subset of the names in force. Widening is not succession: new
authority is admitted by admitting participants that hold it, not by re-declaring a live binding.
An unchanged declaration is refused too, because there is nothing to succeed.

Property: every narrowed declaration names strictly fewer authorities than the one it replaced, and
never one the predecessor did not.

## C4 — a retained dependency keeps its exact tuple

Every name the successor keeps must map to the identical Capability, target Actor, Operation, and
scope it mapped to before. Succession removes dependencies; it does not re-point them.

Property: no retained declared authority changes tuple across a succession.

## C5 — observed use vetoes its own removal

A dependency the member has already exercised, by the attribution and frame rules CBI10 uses, cannot
be narrowed away. Use can contradict a declaration; disuse can never justify one; so observation
appears here only as a veto and never as a permission.

Property: no authority attributed to a delivered interaction is ever dropped by a succession.

## C6 — a refused succession changes nothing

Every refusal leaves the declaration in force exactly as it was, the participant set untouched, and
the member released. CBI11 has no retirement path: a narrowing that cannot be justified is simply
not applied.

Property: the member is released after every CBI11 outcome, and the declaration reported as in force
is the predecessor unless the succession was applied.

## C7 — narrowing permits, it does not perform

A narrowed declaration only changes what CBI9 will admit. Participants whose grants no longer cover
anything declared are not removed by this slice; a CBI9 revision removes them, and it decides that
on the coverage rule it already had.

Property: no succession alters the participant set, the grants in force, or any portable fact.

## C8 — the result names what was dropped and what vetoed it

A succession reports the authorities it removed. A veto reports the exercised authorities that
prevented it, so a refusal is attributable to the interactions that caused it rather than to a
policy the caller cannot see.

Property: every applied succession reports a non-empty dropped set, and every veto reports the
non-empty exercised set that caused it.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate successors over their native CM2, CM5, and PB7 types.
CBI11 is additive: CBI6 through CBI10 are unchanged, and shared material is limited to this contract
and the data-only scenario inventory.

Property: deleting either CBI11 implementation leaves native CM2, CM5, CBI1-CBI10, and Portable
Binding behavior unchanged.

## C10 — evidence remains bounded

CBI11 proves that a declaration narrows only when the Component's own resolution narrows, and never
against observed use, for one released singleton binding. It does not decide whether the successor
generation is itself trustworthy, verify that the Component's new declaration is truthful, observe
anything the host did not record, remove participants, replace the member with a new generation's
member, or provide production identity, policy, distribution, or security.

Property: every CBI11 status statement preserves these limits.

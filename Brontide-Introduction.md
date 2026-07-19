# Brontide: The Idea

*A readable introduction to the Brontide computational model. This document trades precision
for clarity; the precise version is the
[architecture specification](./Brontide-Architecture-0.8.md), and where the two disagree, the
specification wins.*

---

## Computing no longer fits inside a computer

Look at what actually cooperates in a modern working day. A person asks a software system to do
something. That system hands part of the work to a service in another building. An automated
agent hits a question it cannot answer and asks a human. A mouse reports movement to a laptop.
A company closes its accounting period — an action realised by hundreds of programs, none of
which *is* the action.

Every one of these interactions works today, and every one works differently. Programs have
processes. Users have accounts. Devices have drivers. Services have APIs. Agents invent their
own tool protocols. Large business actions often exist only as conventions scattered across the
systems involved. These boundaries are mostly historical accidents, and every pair of worlds
needs its own bridge.

Brontide starts from a different question:

> **What is the smallest common model through which radically different participants can
> cooperate?**

Not the richest model. The smallest. Small enough that a pair of headphones can implement it,
meaningful enough that a corporation can express a financial audit in it.

## The idea in one sentence

> *Actors execute Operations by presenting explicit and bounded Capabilities.*

Three words carry the weight, so here they are in plain language.

An **Actor** is anything that participates: a program, a firmware routine inside a sensor, a
human at a keyboard, an AI agent, an entire company system. Brontide does not claim these are
equivalent — a person is not a process. It claims they can share one model for a specific
thing: *who is allowed to cause what*.

An **Operation** is a named, meaningful action: `Fan.SetSpeed`, `Temperature.Read`,
`Deployment.Approve`, `Database.Migrate`, `Audit.Start`. Note the range — a register write and
a company-wide audit sit in the same list, deliberately. An Operation keeps its meaning
regardless of how much machinery hides underneath it.

A **Capability** is the interesting one. It is authority made into an explicit, inspectable
thing: a grant that says *this holder may execute these Operations, within these limits*. Think
of a key with the rules engraved on it — which doors, until when, and whether you may copy it.

## Keys that can only be filed down

Most systems answer "who may do what" with a list checked by a central guard: an account
database, a permission table, an access-control service. Brontide inverts this. There is no
central guard. Authority is a thing you *hold* and *present*, and the system it is presented to
checks it at its own door.

The consequence that makes everything else work: **authority only ever narrows**.

If you hold a key, you may hand a copy to someone else — but the copy can only be your key
with *more* restrictions filed into it, never fewer. A build system holding deployment approval
for all environments can hand its agent a key restricted to `staging` only. The agent can hand
a sub-worker that same key restricted further to a single deployment window. Nobody, anywhere
in the chain, can file teeth *onto* a key.

This is not enforced by some component comparing permissions — comparing "is this authority
smaller than that authority" is exactly the kind of judgement independent systems get wrong in
independently creative ways. It is enforced *by construction*: a handed-down Capability is
defined as the parent plus added restrictions, and there is no other way to express one.
Narrowing is not checked; it is the only thing that can be said.

Restrictions have one more rule, and it is the security heart of the whole model: **if a system
does not understand a restriction, it refuses**. A restriction written in vocabulary a target
has never seen does not get skipped — it kills the request. Older systems therefore degrade to
*stricter*, never to *wrong*. Every permission system that skipped what it didn't understand
has eventually been walked through by someone who made sure it didn't understand the important
part.

## Authority has a birth, a life, and a death

Where do keys come from in the first place? Brontide's answer is a conservation law. Every
Capability is either **primordial** — created by the system itself at a known, recorded moment
called **Genesis** (switch-on, boot, the moment you plug in a device) — or derived from another
Capability by the narrowing rule. Authority never appears mid-flight. If you follow any key's
history backwards, you always arrive at a recorded beginning.

What about things created during operation — a new file, a new record? The provider that
created it hands you a key *carved from its own*: a service managing a resource space issues
authority over new resources by narrowing its own authority over the whole space. Even
creation mints nothing new.

Authority also *ends*. Grants can carry expiry times or stay valid only while their grantor
actively keeps them alive — the recommended default is that **authority is mortal, and
immortality is the explicit exception**. Doing nothing kills the key; that is the right
failure direction. And when a participant leaves the system entirely, that moment has a name
too: **Terminus**, the mirror of Genesis — just as recorded, just as attributable, with declared
answers for what happens to everything the departed participant held or granted.

Birth recorded, life only narrowing, death declared. At no point does authority exist that
nobody can account for.

## Saying what happened, without lying about it

Cooperation needs more than permission; it needs a shared way to talk about events. Brontide
keeps three ideas apart that most systems blur:

- An **Execution** says *this Operation was attempted, like this*. It may be refused, may
  fail, may succeed — its existence promises none of those.
- An **Event** says *someone asserts this happened*. Deliberate wording: the system records who
  said it, not that it is true.
- An **Outcome** says *this attempt ended, like this* — the final word on one Execution.

The distinctions sound pedantic until you replay things. Replaying an Event repeats a *claim*.
Replaying an Execution may repeat an *effect* — a second fund transfer, a second migration.
Systems that model both as "messages" rediscover this difference in production.

One more separation, small on paper and large in consequence: **receiving an Event grants no
authority to react**. Hearing that the temperature changed does not entitle you to touch the
fan. If you react, you act on your own key, and the record shows you as the initiator. Entire
classes of confused-deputy attacks — tricking a privileged system into using its power on your
behalf — die at this rule.

## Two things cross every boundary, under opposite rules

For independent systems to cooperate, they must agree on the *structure* of what they exchange
without sharing programming languages or wire formats. Brontide's structural contract is called
a **Shape**: a named, versioned description of a value — never the value itself, never the
bytes.

Here lives one of Brontide's most useful discoveries. Every request crosses a boundary carrying
two different things: the *payload* — the data being acted on — and the *authority* — the
presented Capability with its restrictions. These two must be treated by **opposite rules**.

Payload is like a letter: if it contains a word you don't know, you skim past and read the
rest. That tolerance is what lets independently evolving systems keep talking — newer values
carry extra structure, older readers safely ignore it.

Authority is like a contract: a clause you don't understand is not a clause you may skip. If a
restriction arrives carrying structure the checker cannot read, the *restriction* might be the
part it cannot read — skipping it would mean granting more than intended. So the tolerance
rule flips: unknown structure in the authority lane means refusal.

Same boundary, two lanes, opposite failure preferences — because a payload failure costs you a
feature, and an authority failure costs you the model. Brontide states this as a first-class
principle rather than leaving each implementer to discover which lane they are in.

## Was that really a human?

Authority answers *by what right*. A different question matters more every year: *what kind of
thing actually did this?* A remote-control tool may have impeccable permission to move the
pointer — and still must not be able to *look like a mouse*. A consent record is only worth
something if a human, not software holding the human's delegated key, produced the click.

Schemes where suspicious software is supposed to label itself fail immediately: attackers
don't self-label. Brontide inverts the burden. Claiming an origin — "this came from a physical
device", "a human did this" — is itself a privilege that must be granted, and it does **not**
survive handing down. Software that acquires the ability to inject input without receiving
that grant produces effects automatically marked *unverified*. Looking like a device is a
right it was never given. In a world of autonomous agents, "was a human actually in the loop"
becomes a checkable property of the record rather than a hope.

## Small enough for a mouse, big enough for an audit

Every concept above passes a test with a deliberately humble name: the **Embedded Test**. All
of it — actors, keys, narrowing, birth and death of authority — must remain implementable on a
tiny microcontroller with no operating system, no network, no dynamic memory. On such a chip,
the whole authority model can be a compile-time table; checking a key costs an integer
comparison. The test exists to keep the foundation honest: anything that *needs* heavy
machinery is not fundamental and lives in an optional extension instead.

The same eight-term model stretches the other way without modification. `Audit.Start` at a
corporation obeys exactly the rules `Fan.Stop` obeys in firmware: explicit authority,
inspectable history, narrowing delegation, recorded outcomes. The audit's *implementation* may
involve hundreds of services and people; its *meaning and authority* stay visible at the
boundary as one Operation. That single span — one model from a fan to a financial close — is
the claim that makes Brontide different, and the claim its whole evidence programme exists to
test.

## No implementation gets to be the truth

The most common death of a good specification: the first popular implementation becomes the
de facto standard, accident by accident, until nothing else can be built. Brontide defends
against this structurally.

There are **two deliberately independent implementations** — the Reference Stack (C#, the
practical showcase) and the Minimal Stack (F#, the lean counterpoint) — and neither defines
Brontide. The proof of the model is not that both pass the same tests; it is **interchange**:
a component from one stack working inside the other, with neither depending on the other's
private machinery. Two materially different interchange proofs already cross real process
boundaries, exercising failure, refusal, replay, and version mismatch on the way.

The project's culture follows the same instinct. Conformance tests are written to show
*attacks failing*, not just features working: the copied key that doesn't open more doors, the
expired lease that stops a whole delegation chain, the restriction in a newer dialect that an
older checker correctly refuses to skim. Anything unspecified is declared open, so nobody can
build a de facto standard out of an accident.

## Where it stands, honestly

Brontide is version 0.x and says so plainly. Nothing is ratified yet. The registry that tracks
architecture status is machine-checked; claims of implementation are backed by evidence files
and gates that refuse to pass otherwise.

What exists: the full authority model described here, hardened this cycle by an adversarial
review that closed real holes — including the rules for rate-limit budgets under delegation,
strict handling of newer-versioned restrictions at older checkers, and the completed lifecycle
from Genesis to Terminus. What comes next, in decided order: **Channel** (how requests and
authority travel between processes, distilled from the interchange evidence that already
exists), the **Portable Binding** (a wire-level realisation of it), and **Flow** (long-lived
exchanges — streams, subscriptions — as the first fully ratified extension). Bigger questions
— cross-organisation identity, full revocation — are recorded openly rather than promised.

## The point

Strip everything away and Brontide is one conviction:

> Authority should be explicit, inspectable, and only ever narrower — for every participant,
> at every scale, from a fan controller to a financial audit, whether the hand on the key
> belongs to a program, a device, a company, or a person.

Everything else — the eight terms, the two stacks, the tests designed to fail loudly — exists
to find out whether that conviction survives contact with reality. So far, it is surviving.

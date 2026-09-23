# Siebwalde Development Workflow v1

**Version:** 1.0  
**Status:** Baseline approved for implementation  
**Purpose:** Permanent development, review, validation, evidence, Git, resume, and agent-orchestration workflow for the Siebwalde project.

---

## 1. Purpose and scope

### 1.1 Purpose

The Siebwalde Development Workflow defines how development increments move from Product Owner intent to verified and closed repository changes.

Its goals are to:

- allow the AI Project Lead to autonomously orchestrate routine engineering work;
- reduce human micro-management between normal engineering steps;
- preserve explicit human authority over product, safety, physical-hardware, irreversible, and final merge decisions;
- make workflow progression evidence-driven rather than agent-assertion-driven;
- preserve independent implementation, review, and validation boundaries where they add value;
- make interrupted work safely resumable;
- prevent historical documentation from being confused with current project reality;
- preserve evidence provenance;
- improve the agent system through retrospectives, regression tests, and workflow evals rather than repeated ad-hoc prompting.

The intended operating model is:

```text
Human Product Owner
        |
        | objective + autonomy envelope
        v
AI Project Lead
        |
        +--> Architect when required
        +--> Developer
        +--> Integrator when required
        +--> Designer when required
        |
        v
evidence / gates / corrective loops
        |
        v
Human only when genuine authority or product decisions are required
```

### 1.2 Applicability

This workflow applies to:

- product software;
- tests and harnesses;
- architecture and interface changes;
- integration work;
- WPF/UI changes;
- firmware-related development;
- hardware-sensitive changes;
- development tooling and agent infrastructure;
- repository governance changes;
- project documentation changes.

Not every increment requires every workflow state, role, review class, or validation level.

The Project Lead MUST select the smallest workflow and role set that can reliably establish the required result.

### 1.3 Proportionality

Workflow rigor MUST be proportional to behavioral impact, evidence value, and risk.

A trivial non-behavioral change MUST NOT be forced through the same ceremony as a safety-relevant hardware change.

Likewise, a small source diff MUST NOT be treated as low risk merely because it is small.

The workflow distinguishes **required rigor** from **administrative ceremony**. Required review, evidence, and safety boundaries MUST be preserved; duplicate reporting and unnecessary agent involvement SHOULD be avoided.

### 1.4 Out of scope

This document does not contain:

- current project status;
- current test counts;
- current Git revisions;
- complete product requirements;
- detailed system architecture;
- complete historical evidence;
- implementation-specific OpenCode permission syntax;
- detailed role instructions already owned by individual agent contracts.

Those belong in the appropriate current-state, product, architecture, historical, tooling, or role-specific documents.

---

## 2. Governance model and core principles

### 2.1 Human Product Owner

The human Product Owner owns:

- product intent;
- product priorities;
- material product trade-offs;
- material scope decisions;
- risk acceptance;
- physical-hardware authority gates;
- firmware-flash authority;
- destructive-action authority;
- protected-branch merge authority;
- approval of permanent workflow/governance changes that materially change authority or operating semantics.

The Project Lead MUST NOT assume Product Owner authority merely because the technically preferred action appears obvious.

### 2.2 AI Project Lead

The AI Project Lead owns:

- workflow state;
- increment decomposition;
- classification;
- planning;
- acceptance-criteria organization;
- autonomy-envelope interpretation;
- role selection;
- bounded delegation;
- corrective routing;
- evidence orchestration;
- review and validation sequencing;
- workflow checkpoints;
- status reporting;
- closure and retrospective orchestration.

The Project Lead is the workflow and orchestration owner.

It is not the default implementation owner for behavioral product changes.

The Project Lead MAY directly perform:

- repository inspection;
- workflow classification;
- planning;
- evidence aggregation;
- current-state maintenance;
- small administrative or documentation changes;
- other bounded non-behavioral work where delegation would add no meaningful specialization or independence.

### 2.3 Tool permission is not workflow authority

Technical permission to use a tool does not grant workflow authority to use that tool for every purpose.

For example:

```text
bash allowed
```

does not imply:

```text
live hardware allowed
firmware flashing allowed
history rewrite allowed
merge allowed
```

Workflow authorization is determined by:

1. this workflow;
2. the active Increment Contract;
3. the active autonomy envelope;
4. explicit scoped human authority grants where required.

### 2.4 Core principles

The following principles are mandatory.

1. **Evidence advances workflow state; agent assertion does not.**
2. **The Project Lead owns workflow transitions.**
3. **Use the smallest sufficient role set and the least authority necessary.**
4. **Independent review must actually be independent when required.**
5. **Human intervention is reserved for genuine authority, product-decision, unresolved-blocker, and exceptional governance boundaries.**
6. **Routine agent delegation and corrective loops do not require repeated human approval while they remain within the active autonomy envelope.**
7. **Requested, Commanded, Written/Transmitted, Acknowledged, and Observed are distinct semantic boundaries unless the implementation proves otherwise.**
8. **Failure is normally workflow input for autonomous correction, not automatic human escalation.**
9. **Historical documentation is not automatically current truth.**
10. **Agents MUST NOT silently broaden increment scope.**
11. **Agents MUST NOT autonomously rewrite permanent workflow, authority rules, or role contracts merely because a retrospective suggests an improvement.**
12. **Physical process termination is not equivalent to confirmed physical safe state.**
13. **A reviewer reviews a known revision or clearly bounded diff; review validity MUST be reconsidered after material changes.**
14. **Pre-existing human work and unique evidence MUST be preserved.**

### 2.5 Material change and non-trivial increment

An increment is non-trivial when it materially affects one or more of:

- behavioral production code;
- interfaces;
- state ownership;
- lifecycle or concurrency;
- integration behavior;
- evidence-producing test or harness infrastructure;
- physical hardware interaction;
- safety behavior;
- public/operator behavior;
- development workflow;
- agent governance.

Pure spelling, formatting, comment-only, metadata-only, or similarly non-behavioral maintenance MAY be classified as trivial.

---

## 3. Workflow state model

### 3.1 Primary workflow states

The primary workflow states are:

```text
INTAKE
CLASSIFY
PLAN
ANALYZE_DESIGN
IMPLEMENT
SELF_VERIFY
INDEPENDENT_REVIEW
VALIDATION_PREP
VALIDATE
EVIDENCE_COMPLETE
PR_PREP
PR_ACTIVE
MERGE_READY
POST_MERGE_VERIFY
CLOSURE
RETROSPECTIVE
DONE
ABORTED
```

States are conditional.

For example, a trivial documentation-only increment may not require `ANALYZE_DESIGN`, `INDEPENDENT_REVIEW`, `VALIDATION_PREP`, `VALIDATE`, or `PR_ACTIVE`.

### 3.2 Execution status

Execution status is separate from primary workflow state.

Allowed statuses are:

```text
ACTIVE
PAUSED
BLOCKED
WAITING_AUTHORITY
WAITING_PRODUCT_DECISION
UNKNOWN_EXECUTION_STATE
TERMINAL
```

`TERMINAL` applies only when `primary_state` is a terminal state (`DONE` or `ABORTED`); a completed or aborted increment MUST NOT use a non-terminal execution status.

Conversely, the non-terminal statuses (`ACTIVE`, `PAUSED`, `BLOCKED`, `WAITING_AUTHORITY`, `WAITING_PRODUCT_DECISION`, `UNKNOWN_EXECUTION_STATE`) apply only to non-terminal primary states.

Example:

```text
primary_state: VALIDATION_PREP
execution_status: WAITING_AUTHORITY
required_authority: LIVE_HARDWARE
```

This distinction MUST be preserved so interruption or waiting conditions do not destroy the actual engineering state.

### 3.3 Terminal states

`DONE` is the successful terminal state.

`ABORTED` is the deliberate unsuccessful terminal state.

For a terminal `primary_state` (`DONE` or `ABORTED`), `execution_status` MUST be `TERMINAL`. A terminal increment has no pending execution to resume, so `execution_status` MUST NOT be `ACTIVE`, `PAUSED`, `BLOCKED`, `WAITING_AUTHORITY`, `WAITING_PRODUCT_DECISION`, or `UNKNOWN_EXECUTION_STATE`.

An aborted increment MAY still require cleanup, evidence preservation, and retrospective work before it is administratively closed.

### 3.4 Workflow transition rule

A workflow state MAY advance only when:

1. the exit criteria of the current state are satisfied;
2. required evidence exists;
3. required review or validation has completed;
4. the next action is inside the active autonomy envelope or has a valid authority grant;
5. no unresolved finding invalidates the transition.

A tool reporting success, a process exiting with code zero, a commit being created, or an agent reporting `PASS` is not by itself sufficient to advance state.

### 3.5 Corrective backward transitions

There is no separate `CORRECTIVE_LOOP` state.

On failure:

```text
FAIL
  |
  v
classify failure
  |
  v
identify earliest workflow state whose assumptions/output are invalid
  |
  v
return to that state
```

Examples:

```text
local implementation defect
    -> IMPLEMENT

state-ownership defect
    -> ANALYZE_DESIGN

acceptance criterion misunderstood
    -> PLAN

material product ambiguity
    -> PLAN + WAITING_PRODUCT_DECISION
```

---

## 4. Intake, classification, and Increment Contract

### 4.1 Intake

The Product Owner normally provides:

- objective;
- requested outcome;
- important scope constraints;
- known feature-specific context;
- requested stopping point or autonomy level where relevant.

The Project Lead MAY refine acceptance criteria and technical scope boundaries as long as this does not materially change product intent.

### 4.2 Change classification

The Project Lead MUST classify the increment before material implementation begins.

Typical change classes are:

```text
DOCUMENTATION
PRODUCT_SOFTWARE
TEST_HARNESS
ARCHITECTURE_INTERFACE
UI
WORKFLOW_TOOLING
GOVERNANCE_CHANGE
FIRMWARE
HARDWARE_SENSITIVE
```

More than one classification MAY apply.

### 4.3 Risk classification

At minimum:

```text
NORMAL
ELEVATED
SAFETY_RELEVANT
```

Risk classification affects review, validation, evidence, and authority requirements.

### 4.4 Independent review classification

Independent review class is separate from validation class.

```text
R0 — No independent review required
R1 — Independent software review
R2 — Independent integration/system review
```

#### R0

Appropriate for changes with no intended product-behavior impact and no material evidence/governance impact, such as:

- spelling/formatting;
- comment-only changes;
- simple documentation correction;
- mechanical metadata updates;
- low-risk test additions that do not alter expected product semantics and are not themselves the primary acceptance evidence.

#### R1

**R1 is the default for behavioral production-software changes.**

This is deliberate.

R1 provides a second independent check without requiring full integration ceremony. It SHOULD remain lightweight and proportional for small local changes.

A small R1 change does **not** imply that Integrator must:

- rerun the full repository test suite;
- reproduce every Developer action;
- perform architecture review;
- perform integration or physical validation.

A proportional R1 review normally means:

- inspect the bounded final diff/revision;
- inspect the affected execution path;
- challenge acceptance/evidence claims;
- independently rerun or inspect the most relevant targeted checks;
- identify untested edge cases or unsupported claims.

The Project Lead MAY classify a change as R0 only when it can state why there is no intended behavioral production impact and no material evidence/governance impact.

Changes to tests or harnesses that alter expected semantics, drive physical behavior, or materially form acceptance evidence SHOULD receive at least R1 and MAY require R2.

#### R2

R2 is required for material changes involving one or more of:

- cross-component behavior;
- lifecycle/startup/shutdown/restart;
- concurrency or shared-state ownership;
- protocol or interface semantics;
- shared control infrastructure with broad impact;
- physical hardware command paths;
- safety-relevant behavior;
- evidence infrastructure whose correctness materially affects a safety/product conclusion.

The Project Lead MUST record review class and rationale.

### 4.5 Validation classification

Validation class describes the strongest expected execution level.

```text
V0 — Inspection only
V1 — Build / automated software execution
V2 — Emulator / simulator / harness
V3 — Integration / runtime execution
V4 — Physical hardware
```

Examples:

```text
normal local behavioral fix:
review = R1
validation = V1

cross-component protocol change:
review = R2
validation = V3

physical safety-path change:
review = R2
validation = V4
```

### 4.6 Acceptance criteria

Acceptance criteria MUST describe the behavior or result that must be demonstrated.

They MUST NOT be weakened merely because evidence is difficult to obtain.

Acceptance criteria MAY be amended when:

- the original requirement was materially misunderstood;
- a verified architectural constraint requires re-planning;
- Product Owner scope changes;
- a Product Owner-approved exception applies.

Material amendments MUST be recorded.

### 4.7 Increment Contract

At the end of `PLAN`, the Project Lead establishes the Increment Contract.

For non-trivial work it contains at minimum:

```text
Increment
Objective
Acceptance Criteria
Scope
Explicit exclusions where useful

Change class
Risk class
Review class
Validation class

Target branch
Working branch / branch strategy

Autonomy envelope
Expected human authority gates

Required roles
Known constraints
Baseline failures

Definition-of-Done requirements
```

For trivial work the Increment Contract MAY be compact, but MUST still make objective, scope, risk/review classification, and stopping point clear.

The Increment Contract is the operational baseline for the remainder of the increment.

### 4.8 Contract amendment and scope change

Material changes to:

- product behavior;
- acceptance criteria;
- scope;
- validation level;
- authority requirements;
- risk classification;

MUST be treated as an Increment Contract amendment.

The Project Lead MUST NOT silently replace previous requirements.

### 4.9 Baseline failures

Pre-existing failures MUST be distinguished from increment-induced failures.

Example:

```text
before:
342 pass
1 known pre-existing fail

after:
342 pass
same known pre-existing fail
```

is materially different from introducing a new failure.

Pre-existing failures do not automatically become increment scope unless:

- they prevent reliable validation;
- they are causally coupled to the increment;
- or the Increment Contract is explicitly amended.

---

## 5. Autonomy envelope

### 5.1 Model

Autonomy is modeled as:

```text
Base Profile
+ explicit capabilities
- explicit exclusions
+ scoped authority grants
```

It MUST NOT be inferred from tool permissions.

### 5.2 Base profiles

#### ANALYSIS

Allows:

- inspection;
- investigation;
- planning;
- architecture analysis;
- bounded read-only delegation.

Does not allow behavioral source implementation unless separately authorized.

#### SOFTWARE_IMPLEMENTATION

Allows:

- source changes;
- tests;
- documentation tied to implementation;
- builds;
- software tests;
- emulator/simulator tests;
- local branch operations;
- local commits;
- agent delegation;
- corrective loops.

Does not by itself authorize:

- live hardware;
- firmware flash;
- protected remote actions;
- merge;
- destructive recovery.

#### PR_READY

Includes software implementation plus:

- required independent review;
- required software/integration validation;
- branch hygiene;
- documentation;
- evidence completion;
- PR preparation.

It does not by itself authorize remote PR creation.

#### PR_EXECUTION

Includes PR-ready work plus, where repository setup permits:

- remote branch push;
- PR creation;
- normal CI/review correction loops.

Merge remains separately human-authorized.

### 5.3 Optional capabilities

Capabilities MAY be added independently, including:

```text
REMOTE_GIT
CREATE_PR
LIVE_HARDWARE
FIRMWARE_FLASH
```

Capabilities are orthogonal.

For example:

```text
PR_READY + LIVE_HARDWARE
```

does not imply `CREATE_PR`.

Likewise:

```text
PR_EXECUTION
```

does not imply `LIVE_HARDWARE`.

### 5.4 Least-authority rule

An allowed action does not have to be used.

The Project Lead MUST use the least authority and smallest role set reasonably necessary to complete the increment.

### 5.5 Always-gated or restricted actions

The following require explicit workflow authority and are never inferred from ordinary edit/bash permissions:

- live physical actuation;
- firmware flash;
- destructive recovery;
- shared/evidence-bearing history rewrite;
- force push;
- protected-branch merge;
- deletion of evidence-bearing branches;
- material Product Owner decisions.

### 5.6 Approved exceptions and risk waivers

The Product Owner MAY explicitly accept a known limitation or requirement exception.

A waiver MUST record:

```text
affected requirement
actual evidence status
known limitation/consequence
applicable revision/scope
Product Owner acceptance
```

A waiver MUST NOT convert:

```text
FAIL
NOT_PROVEN
```

into:

```text
PASS
PROVEN
```

Instead:

```text
AC5: NOT_PROVEN
Disposition: ACCEPTED_EXCEPTION
```

MAY allow an increment to proceed where appropriate.

An unknown or uncontrolled physical safety state MUST NOT be represented as passed merely through risk acceptance.

---

## 6. Role routing and delegation

### 6.1 Project Lead

The Project Lead:

- owns workflow state;
- chooses required roles;
- issues bounded tasks;
- interprets role results;
- manages evidence invalidation;
- decides workflow transitions;
- escalates only where workflow authority requires it.

### 6.2 Developer

Developer is normally required for behavioral implementation.

Developer owns:

```text
source investigation
implementation
targeted tests
build
software self-verification
implementation report
```

Developer MUST distinguish:

- inspected behavior;
- test-executed behavior;
- runtime-observed behavior;
- assumptions and uncertainty.

Developer MUST NOT be the sole independent accepting reviewer of its own behavioral change when R1/R2 applies.

### 6.3 Architect

Architect is required when one or more of the following materially change:

- state ownership;
- lifecycle ownership;
- concurrency model;
- subsystem boundary;
- public/internal interface boundary;
- dependency direction;
- safety responsibility;
- persistent state model;
- material architecture trade-off.

Architect SHOULD also be involved when Developer investigation exposes a design blocker.

Architect is not mandatory for routine local implementation defects.

Architect evaluates design correctness, not final feature acceptance.

### 6.4 Integrator

Integrator owns independent behavioral/integration verification.

Integrator is normally used for R1 and required for R2.

For a small R1 change, Integrator SHOULD perform a bounded targeted review rather than reproduce the full Developer workflow.

Integrator may perform:

- independent code review;
- targeted test/evidence verification;
- emulator/harness execution;
- integration validation;
- end-to-end validation;
- physical validation when authorized.

Integrator MUST review a known revision or explicitly bounded diff.

Integrator MUST NOT silently modify the production implementation to fix a defect and then accept its own fix as independent validation.

Concrete implementation defects return to Developer.

Architectural defects return through Project Lead to Architect and then Developer.

A harness limitation MUST NOT automatically be classified as a product defect.

### 6.5 Designer

Designer is conditionally used for UI/UX-dominant changes such as:

- substantial interaction redesign;
- new operator workflow;
- complex visual state presentation;
- safety-relevant operator interaction;
- significant WPF page/layout redesign.

Designer does not replace Integrator when functional or safety validation is required.

### 6.6 Governance changes

Changes to:

- this workflow;
- role contracts;
- authority semantics;
- permanent agent permissions;
- governance architecture;

are classified as `GOVERNANCE_CHANGE`.

Governance changes require at least independent consistency review.

Changes to workflow-state semantics, authority boundaries, role ownership, or safety governance SHOULD be treated as R2-equivalent governance review even when no product runtime code changes.

Changes that relax authority boundaries require Product Owner approval.

### 6.7 Segregation of duties

The agent materially implementing a behavioral change MUST NOT be the sole independent reviewer accepting that change when R1/R2 applies.

Architect review does not replace Integrator verification.

Designer review does not replace functional verification.

Developer self-verification does not replace required independent review.

### 6.8 Delegation Context Contract

Every delegated task MUST receive a bounded context package sufficient to perform the role.

A context package SHOULD contain:

```text
Role
Objective
Scope
Acceptance criteria relevant to the role
Known source paths/symbols where useful
Known evidence
Constraints
Explicit do-not-do boundaries
Expected output
```

A subagent MUST NOT be assumed to possess the full parent-agent context.

### 6.9 Agent Result Contract

Role results SHOULD end with:

```text
ROLE RESULT

Verdict:
PASS | FAIL | BLOCKED | NEEDS_DECISION

Scope evaluated:
...

Evidence:
...

Findings:
...

Unverified:
...

Recommended next workflow state:
...
```

The recommended next state is advisory.

Only the Project Lead changes workflow state.

### 6.10 Parallelism and workspace isolation

Read-only investigation MAY be parallelized when useful.

Concurrent modifications to the same working tree are prohibited unless explicitly isolated.

Parallel implementation requires:

- separate branches/worktrees;
- clear ownership boundaries;
- explicit integration step;
- later combined verification.

Physical hardware-driving validation SHOULD have only one active controlling session unless a specifically designed multi-session test requires otherwise.

---

## 7. Implementation and corrective workflow

### 7.1 Normal development flow

A typical behavioral software increment is:

```text
IMPLEMENT
   |
   v
SELF_VERIFY
   |
   v
INDEPENDENT_REVIEW
   |
   v
required validation
   |
   v
EVIDENCE_COMPLETE
```

### 7.2 Failure classification

Meaningful failures SHOULD be classified as one of:

```text
IMPLEMENTATION_DEFECT
TEST_DEFECT
DESIGN_DEFECT
REQUIREMENT_AMBIGUITY
INTEGRATION_DEFECT
VALIDATION_TOOL_DEFECT
ENVIRONMENT_FAILURE
DOCUMENTATION_CONFLICT
SCOPE_DISCOVERY
SAFETY_CONCERN
UNKNOWN_CAUSE
```

### 7.3 Earliest-invalid-state rule

On failure, the Project Lead MUST return the workflow to the earliest state whose assumptions or outputs have become invalid.

Examples:

```text
implementation condition wrong
    -> IMPLEMENT

ownership model wrong
    -> ANALYZE_DESIGN

acceptance criteria wrong
    -> PLAN

material product ambiguity
    -> PLAN + WAITING_PRODUCT_DECISION
```

### 7.4 Evidence and review invalidation

After corrective work, the Project Lead MUST determine which previous evidence and reviews remain valid.

A later change invalidates earlier evidence only where the evidence depended on changed assumptions, source, configuration, or runtime behavior.

Unaffected evidence SHOULD be retained.

### 7.5 Re-review

Any review whose assumptions were invalidated by corrective work MUST be repeated.

The workflow MUST NOT claim independent review based on an earlier revision when the reviewed behavior materially changed.

### 7.6 Scope discovery

Discovered work MUST be classified as:

```text
REQUIRED_FOR_ACCEPTANCE
WITHIN_SCOPE_SUPPORTING_WORK
MATERIAL_SCOPE_EXPANSION
ADJACENT_IMPROVEMENT
PROCESS_IMPROVEMENT
```

Material scope expansion requires Increment Contract amendment and, where product meaning changes, Product Owner decision.

### 7.7 No silent scope absorption

Useful adjacent work is not automatically part of the increment.

Examples:

```text
required to satisfy acceptance criteria
    -> include

independent product improvement
    -> backlog candidate

agent/workflow improvement
    -> process-improvement backlog

unrelated refactor
    -> defer unless explicitly accepted
```

Product code and workflow/tooling changes SHOULD NOT be mixed in the same product increment unless necessary and explicitly accepted.

---

## 8. Evidence model and Definition of Done

### 8.1 Evidence levels

#### E1 — Inspection evidence

Examples:

- source inspection;
- interface definition;
- configuration inspection;
- Git diff;
- branch/commit state.

#### E2 — Software execution evidence

Examples:

- builds;
- unit tests;
- component tests;
- static checks;
- software-only harness;
- simulator/emulator.

#### E3 — Integration/runtime evidence

Examples:

- end-to-end execution;
- runtime logs;
- integration traces;
- lifecycle behavior;
- multi-component harness evidence.

#### E4 — Physical observation evidence

Examples:

- detected physical device;
- fresh hardware readback;
- physical register value;
- measured electrical state;
- observed motor/actuator behavior.

### 8.2 Claim/evidence boundary

Evidence MUST only support claims at boundaries it actually observes.

For example:

```text
command requested
```

does not necessarily prove:

```text
command transmitted
```

and:

```text
command transmitted
```

does not necessarily prove:

```text
hardware applied command
```

unless the transport explicitly provides stronger semantics.

The following concepts MUST remain distinct where applicable:

```text
Requested
Commanded
Written / Transmitted
Acknowledged
Observed
```

### 8.3 Acceptance-criterion-to-evidence mapping

Before `EVIDENCE_COMPLETE`, every acceptance criterion MUST have an explicit evidence disposition:

```text
AC1 -> PROVEN by ...
AC2 -> PROVEN by ...
AC3 -> NOT_PROVEN / BLOCKED / ACCEPTED_EXCEPTION
```

Evidence volume is not a substitute for evidence relevance.

### 8.4 Evidence provenance

Evidence MUST be attributable to the system revision that produced it.

For important validation, provenance SHOULD include as applicable:

```text
branch
commit/revision
build configuration
configuration identity
binary/artifact identity
test/harness identity
hardware/session identity
```

Safety-sensitive or difficult-to-reproduce evidence MAY require stronger artifact identity such as a file hash.

### 8.5 Evidence freshness

Evidence remains valid only while the assumptions, code, configuration, and environment on which it depends remain sufficiently unchanged.

After material corrective work, evidence freshness MUST be reassessed.

### 8.6 Evidence Anchors

A revision becomes an **Evidence Anchor** when significant validation evidence is explicitly tied to that revision.

Evidence Anchors MUST remain reachable and immutable enough to preserve provenance.

Shared or evidence-bearing history containing an Evidence Anchor MUST NOT be silently rewritten.

### 8.7 Evidence status

Evidence status is separate from workflow and agent status.

Allowed evidence dispositions are:

```text
PROVEN
PARTIALLY_PROVEN
NOT_PROVEN
NOT_APPLICABLE
```

An accepted Product Owner waiver does not change evidence status.

### 8.8 Evidence Packet

Non-trivial increments SHOULD have a compact Evidence Packet containing:

```text
Increment
Revision

Acceptance Criteria
  AC1 -> evidence
  AC2 -> evidence

Software Verification
Independent Review
Integration / Physical Validation

Known Limitations
Accepted Exceptions
Unverified Claims
Invalidated/Replaced Evidence

Overall Evidence Status
```

The Evidence Packet does not require a separate file. It MAY live in an Active State record, PR description, validation record, or closure summary as long as it remains reviewable and durable enough for the increment.

### 8.9 Definition-of-Done hierarchy

#### DoD-S — Software Implementation Done

Requires, as applicable:

- intended implementation complete;
- scope understood;
- required tests added/updated;
- required build succeeds;
- required software tests pass;
- known implementation limitations recorded;
- inspected versus executed evidence distinguished.

#### DoD-R — Independent Review Done

Requires:

- correct review class;
- independent reviewer where R1/R2 applies;
- relevant paths/evidence reviewed;
- findings classified;
- required findings resolved;
- invalidated review repeated;
- no unsupported `PASS`.

R1 depth SHOULD be proportional to the bounded change and does not require full workflow duplication.

#### DoD-V — Validation Done

Requires:

- required validation level executed;
- expected outcome observed;
- unexpected behavior recorded;
- provenance known;
- evidence interpreted at the correct semantic boundary.

For physical validation, additional requirements in Section 9 apply.

#### DoD-E — Evidence Complete

Requires:

- every acceptance criterion has a disposition;
- evidence quality matches the claim;
- evidence belongs to the final relevant revision;
- required independent review passed;
- required validation passed;
- no unresolved safety-relevant finding;
- limitations and exceptions are explicit.

#### DoD-PR — PR Ready

Requires:

- final diff/scope audit;
- unrelated changes absent;
- branch/history suitable for review;
- documentation updated;
- Evidence Packet available;
- known limitations included;
- PR title/description prepared.

#### DoD-M — Merge Ready

Requires:

- PR content matches reviewed scope;
- required CI passed;
- review findings resolved;
- final revision covered by current evidence;
- no evidence-invalidating change remains unvalidated.

`MERGE_READY` does not authorize merge.

#### DoD-C — Closure Done

Requires as applicable:

- expected change landed on target branch;
- post-merge state verified;
- temporary test processes cleaned;
- durable docs updated;
- backlog/current-state updated where relevant;
- retrospective completed where required.

---

## 9. Validation and physical hardware

### 9.1 Validation levels

Validation SHOULD use the lowest level capable of reliably proving the claim.

Increasing strength:

```text
V0 inspection
V1 software execution
V2 emulator/harness
V3 integration/runtime
V4 physical hardware
```

Hardware evidence MUST NOT be required for claims that can be fully established at lower levels.

Conversely, software-only evidence MUST NOT be used to overclaim physical behavior.

### 9.2 Integrator ownership

Independent integration and physical validation is owned by Integrator.

Developer MAY prepare test support but MUST NOT be the sole accepting validator of its own safety/hardware behavior.

### 9.3 Hardware authority

Before V4 execution:

- `LIVE_HARDWARE` authority MUST be valid;
- authority MUST be scoped;
- target hardware MUST be known;
- revision MUST be known;
- test purpose MUST be known.

### 9.4 Firmware-flash boundary

`LIVE_HARDWARE` does not imply `FIRMWARE_FLASH`.

If a normal startup or validation path can implicitly flash firmware, that behavior MUST be classified before execution.

If physical validation requires automatic flashing:

```text
LIVE_HARDWARE
+
FIRMWARE_FLASH
```

authority is required unless an approved non-flashing path is used.

### 9.5 Operator in the loop

Live physical validation MUST preserve an appropriate operator-in-the-loop model.

The operator MUST know:

- what action will occur;
- expected physical effect;
- manual stop method;
- cleanup/neutralization procedure.

### 9.6 Session, PID, stop, and timeout

Hardware-driving processes MUST have, where technically practical:

- identifiable process/session ownership;
- bounded runtime;
- timeout;
- manual stop path;
- visible process state.

Process exit MUST NOT be treated as evidence of physical neutralization.

### 9.7 Cleanup and neutral state

Physical validation MUST define cleanup before execution.

Where relevant, cleanup includes:

- neutral/stop commands;
- physical safe-state observation;
- process shutdown;
- session release.

A completed test run is not complete until required cleanup has been performed.

### 9.8 Unknown physical state

If a session or tool fails after a side-effecting hardware action and the physical result is unknown:

```text
execution_status = UNKNOWN_EXECUTION_STATE
```

The workflow MUST first re-observe or establish safe state.

It MUST NOT assume success or failure merely from process termination.

### 9.9 Safety anomaly handling

When unexpected behavior may violate a safety invariant:

1. stop further unnecessary actuation;
2. restore or confirm safe state where practical;
3. preserve available evidence;
4. classify the anomaly;
5. invalidate affected assumptions;
6. resume only after the concern is understood sufficiently for safe continuation.

Repeated live attempts MUST NOT be used casually to investigate an uncontrolled safety condition.

---

## 10. Git, workspace, branch, and PR lifecycle

### 10.1 Pre-existing workspace protection

Before modifying the workspace, the Project Lead MUST inspect repository state.

Pre-existing or unrelated human work MUST NOT be:

- discarded;
- overwritten;
- silently reset;
- silently absorbed into increment commits.

If necessary, work SHOULD be isolated using:

- a separate branch;
- a separate worktree;
- another clean checkout.

Ambiguous ownership of important uncommitted work SHOULD cause a blocker or human clarification rather than destructive normalization.

### 10.2 Primary increment branch

A non-trivial increment SHOULD use one coherent primary branch.

Typical forms:

```text
feature/<name>
fix/<name>
chore/<name>
docs/<name>
```

The target branch and branch purpose MUST be part of the Increment Contract.

### 10.3 Product vs workflow/tooling branches

Product changes and agent/workflow infrastructure SHOULD be developed in separate increments.

A product feature branch SHOULD contain only:

- required product source;
- tests/harness work required for that product change;
- feature-relevant documentation.

General workflow/tooling improvements discovered during product development SHOULD normally become separate process-improvement work.

### 10.4 Local commits

Within an authorized software-development envelope, the Project Lead MAY create local commits autonomously.

Commits SHOULD represent meaningful reviewable or evidence-relevant boundaries.

Artificial commit fragmentation is not required.

### 10.5 Local non-evidence history normalization

Local history MAY be normalized autonomously only when all of the following are true:

- history is unpublished;
- no affected revision is an Evidence Anchor;
- no review references the revision;
- no authority grant is bound to the revision;
- no unique work/evidence can be lost.

### 10.6 Shared or evidence-bearing history

History MUST NOT be autonomously rewritten when it is:

- pushed/shared;
- review-referenced;
- evidence-bearing;
- authority-referenced;
- otherwise important to provenance.

Force push remains explicitly gated.

### 10.7 Clean PR branch exception

A separate clean reviewer-facing branch MAY be constructed when:

- the evidence branch contains unrelated changes;
- evidence history must be preserved;
- reviewer scope cannot otherwise be made clean.

This is an exception, not the standard workflow.

When used, the Project Lead MUST establish sufficient equivalence between the validated implementation and reviewer-facing change.

### 10.8 Remote push and PR creation

Remote push and PR creation require the corresponding autonomy capability.

`PR_READY` by itself does not imply remote side-effect authority.

### 10.9 PR, CI, and review corrections

Once a PR is active:

```text
PR_ACTIVE
```

normal CI and review findings MAY enter autonomous corrective loops when the required corrective work remains inside the autonomy envelope.

Reviewer suggestions MUST be classified as:

```text
defect
acceptance issue
optional improvement
scope expansion
product decision
```

A reviewer does not automatically gain Product Owner authority over increment scope.

### 10.10 Merge readiness

Immediately before `MERGE_READY`, Project Lead MUST verify:

- correct target branch;
- expected changed files;
- no accidental workflow/tooling changes;
- final revision covered by evidence;
- required review current;
- CI current;
- unresolved findings absent or explicitly accepted.

### 10.11 Merge action

Merge is a side-effecting transition, not a durable workflow state.

Normal flow:

```text
MERGE_READY
   |
   | explicit human merge authority
   | merge action
   v
POST_MERGE_VERIFY
```

If the merge operation is interrupted:

```text
primary_state = MERGE_READY
execution_status = UNKNOWN_EXECUTION_STATE
```

The repository MUST be inspected before retrying.

### 10.12 Default merge strategy

The default Siebwalde v1 merge strategy is a normal merge commit unless the Increment Contract explicitly states otherwise.

This preserves increment and evidence history.

### 10.13 Branch retention

An evidence-bearing branch MUST NOT be automatically deleted.

A branch MAY later become a deletion candidate when:

- all required Evidence Anchors remain reachable elsewhere;
- unique evidence history has been durably preserved;
- no workflow requirement depends on the branch.

Deletion of a branch classified as evidence-bearing remains human-authorized in Workflow v1.

---

## 11. Human authority gates

### 11.1 Authority types

Authority gates include:

```text
PRODUCT_DECISION
LIVE_HARDWARE
FIRMWARE_FLASH
DESTRUCTIVE_ACTION
REMOTE_PUSH
CREATE_PR
HISTORY_REWRITE
MERGE
EVIDENCE_BRANCH_DELETE
```

Some Git authorities MAY already be included in an active autonomy profile such as `PR_EXECUTION`.

### 11.2 Prepare before gate

The Project Lead MUST complete all safe authorized preparation before requesting human authority.

Example before hardware authority:

- implementation complete;
- software tests complete;
- required independent review complete;
- validation plan prepared;
- expected observations known;
- stop/cleanup procedure known.

### 11.3 Bounded authority request

An authority request SHOULD contain:

```text
Requested action
Reason
Revision
Current workflow state
Evidence already complete
Expected side effects
Scope of requested authority
```

The Product Owner should not need to reconstruct the entire increment merely to decide a gate.

### 11.4 Scope and revision binding

Authority grants are scoped to the approved action.

Where relevant they are also bound to:

- increment;
- revision;
- test plan;
- target hardware;
- session/purpose.

### 11.5 Non-transitive authority

Authority is not transitive.

Examples:

```text
CREATE_PR != MERGE
LIVE_HARDWARE != FIRMWARE_FLASH
MERGE != BRANCH_DELETE
```

### 11.6 Authority invalidation

Material changes to the object of approval MAY invalidate authority.

Example:

```text
merge approved for revision ABC
Developer changes behavioral code -> revision DEF
```

Previous merge approval is no longer assumed valid.

The Project Lead MUST determine whether changed content invalidates:

- review;
- evidence;
- authority.

### 11.7 Product decisions

A Product Owner decision is required when multiple technically valid alternatives differ materially in:

- product behavior;
- operator expectation;
- compatibility commitment;
- roadmap;
- risk acceptance;
- material scope.

Technical uncertainty alone is not automatically a Product Owner decision.

### 11.8 Merge authority

Merge to the protected product branch remains explicitly human-authorized in Workflow v1.

The Project Lead MAY autonomously bring work to `MERGE_READY`.

It MUST stop before merge unless explicit current authority exists.

---

## 12. Current state, source of truth, and resume

### 12.1 Durable current state

`docs/current-state.md` is a compact durable projection of current project reality.

It SHOULD contain information such as:

- current validated product capabilities;
- relevant validated baselines;
- important active limitations;
- important project blockers;
- open Product Owner decisions;
- major active development direction.

It SHOULD NOT be used as a per-agent workflow journal.

### 12.2 Active State Manifest

Operational workflow state SHOULD be maintained separately in an Active State Manifest.

The implementation format may be machine-readable.

Conceptually it contains:

```text
increment
objective
target_branch
working_branch
verified_revision

primary_state
execution_status

autonomy_envelope

acceptance_criteria_status

role_status
review_class
validation_class

evidence_completed
evidence_invalidated

open_findings
pending_authority
authority_validity

blockers
working_tree_state

next_safe_action
relevant_references
```

### 12.3 Typed source authority

There is no single universal document-precedence ladder.

Authority depends on the type of claim.

| Claim | Primary authority |
|---|---|
| Current Git branch/revision | actual Git state |
| Current implementation behavior | source plus executed evidence |
| Whether tests pass | current test/CI execution |
| Physical behavior | physical/runtime evidence |
| Product intent | Product Owner and confirmed product requirements |
| Durable product decision | most recent valid decision record |
| Architecture intent | valid architecture/decision record checked against current implementation |
| Active workflow state | verified Active State Manifest |
| Historical context | historical handoff/decision/evidence docs |
| Project snapshot | `docs/current-state.md` |

### 12.4 Conflict resolution

When sources conflict materially:

1. identify the type of claim;
2. determine its authoritative source;
3. verify current reality;
4. classify stale or historical information;
5. correct current documentation when appropriate.

Agents MUST NOT silently reconcile conflicting sources by invention.

A product requirement conflicting with current implementation MUST be treated as a requirement/implementation mismatch until clarified.

### 12.5 Startup reading order

A normal Project Lead session SHOULD read:

```text
AGENTS.md
docs/development-workflow.md
docs/current-state.md
Active State Manifest if present
actual Git state
```

Then only targeted relevant material:

- relevant backlog item;
- relevant decisions;
- relevant architecture/component docs;
- relevant historical evidence.

Large historical documents SHOULD NOT be loaded in full by default merely because they exist.

### 12.6 Checkpoint rules

The Active State Manifest SHOULD be updated when a material workflow transition changes what is needed to resume safely, and MUST be current before:

- waiting for human authority;
- planned session handoff;
- expected context exhaustion;
- long external wait;
- risky side-effecting execution.

Typical useful checkpoints also include:

- plan complete;
- Developer complete;
- significant review result;
- corrective fix complete;
- evidence complete;
- PR ready;
- merge ready.

It does not need updating after every tool call.

### 12.7 Resume protocol

On resume:

1. load governance and workflow rules;
2. load `docs/current-state.md`;
3. load Active State Manifest if available;
4. inspect actual repository state;
5. compare recorded and actual branch/revision/status;
6. verify completed evidence still belongs to the relevant revision;
7. verify authority grants are still valid;
8. classify unresolved side effects;
9. restore workflow state only after consistency is established;
10. resume from the earliest state whose requirements are not yet proven.

Recorded workflow state is a resume hint until verified against reality.

### 12.8 Manifest loss and recovery

The workflow MUST remain reconstructable if the local Active State Manifest is lost.

Recovery sources include:

- Git history;
- Increment Contract;
- review/evidence records;
- durable documentation;
- PR state where applicable.

The local manifest exists for fast resume, not as the sole durable repository of project truth.

### 12.9 Context exhaustion

When remaining context is insufficient to safely perform the next bounded action plus required verification and cleanup, the Project Lead SHOULD checkpoint rather than start that action.

For:

- live hardware;
- firmware flash;
- destructive repository actions;

sufficient context MUST remain for the complete operation including cleanup and evidence handling before execution begins.

### 12.10 Interrupted side effects

After interruption of a side-effecting operation, current reality MUST be observed before retry.

Examples:

```text
git push interrupted
    -> inspect remote first

PR creation interrupted
    -> determine whether PR exists first

hardware command interrupted
    -> determine physical/current state first
```

Blind retries are prohibited where duplicate or unsafe side effects are possible.

---

## 13. Failure, blocking, and escalation

### 13.1 Status semantics

#### FAIL

Evidence demonstrates that a requirement is not satisfied.

#### BLOCKED

Required progress or evidence cannot currently be obtained.

#### NEEDS_DECISION

Progress requires a material Product Owner or governance decision.

#### UNKNOWN_EXECUTION_STATE

A side effect may or may not have occurred and current reality must be re-observed.

#### ABORTED

The increment is deliberately terminated.

### 13.2 Failure is normally autonomous workflow input

A `FAIL` does not automatically require human escalation.

The Project Lead SHOULD first:

1. classify failure;
2. determine earliest invalid state;
3. route to the appropriate role;
4. execute corrective work;
5. repeat invalidated verification/review.

### 13.3 Blockers

A blocker record MUST identify:

```text
Blocked state
Reason
Evidence
Unblock condition
Work that may continue independently
Resume state/action
```

A blocker freezes only dependent work.

Independent safe work MAY continue.

### 13.4 Stagnation

Corrective loops MAY continue while each iteration:

- produces materially new evidence;
- narrows the cause;
- resolves a finding;
- makes measurable bounded progress.

Stagnation exists when, for example:

- the same failure recurs without new evidence;
- a disproven fix is repeated;
- root cause cannot be narrowed;
- corrections create equivalent regressions;
- scope expands materially beyond the Increment Contract;
- available evidence cannot distinguish competing causes;
- tooling/environment prevents further progress.

There is no arbitrary fixed retry count.

### 13.5 Internal workflow escalation

The Project Lead MAY autonomously escalate internally, for example:

```text
Developer -> Architect
R1 -> R2
V1 -> V2/V3 preparation
```

provided the resulting work remains inside the autonomy envelope.

### 13.6 Authority escalation

Where the technical next step is known but outside authority:

```text
execution_status = WAITING_AUTHORITY
```

Examples:

- hardware validation;
- firmware flash;
- merge;
- destructive recovery.

### 13.7 Decision escalation

Where multiple valid technical choices have material product consequences:

```text
execution_status = WAITING_PRODUCT_DECISION
```

The Project Lead SHOULD provide:

- options;
- technical consequences;
- relevant evidence;
- recommendation where technically justified;
- specific decision needed.

### 13.8 Safety escalation

Any observation suggesting a safety invariant may no longer hold invalidates assumptions for subsequent affected live validation until the concern has been:

- contained;
- evidenced;
- classified;
- sufficiently understood.

Potentially unsafe repeated actuation MUST NOT be used as an unbounded debugging strategy.

### 13.9 Environment and tool failure

Examples:

- CI outage;
- package source outage;
- tool crash;
- unavailable serial port.

These MUST NOT automatically be classified as product failures.

Environment failures MAY become blockers.

### 13.10 Abort

The Project Lead MAY recommend abort when:

- objective is no longer valid;
- required dependency is unavailable indefinitely;
- solution scope/risk is disproportionate;
- Product Owner cancels the increment.

On abort:

- active processes must be stopped safely;
- useful evidence preserved;
- branch/revision recorded;
- cleanup performed;
- retrospective performed for non-trivial work where useful.

---

## 14. Closure, durable knowledge, and improvement

### 14.1 Post-merge verification

After authorized merge, the Project Lead verifies as applicable:

- expected target branch;
- expected merge result;
- remote repository state;
- post-merge CI;
- local working state;
- temporary process cleanup.

Only then may workflow advance to `CLOSURE`.

### 14.2 Closure

Closure MAY include:

- backlog update;
- durable documentation update;
- current-state update;
- durable decision logging;
- branch/evidence retention classification;
- cleanup of temporary artifacts.

`merged` does not automatically mean `closed`.

### 14.3 Durable decision updates

The Project Lead SHOULD determine whether an increment introduced a durable decision.

If yes, the appropriate decision record MUST be updated.

Do not create decision records for routine implementation details that have no durable architectural/product meaning.

### 14.4 Current-state update

`docs/current-state.md` SHOULD be updated when durable project reality materially changes.

It SHOULD describe the state represented by the repository revision, not act as a minute-by-minute workflow log.

Temporary workflow data such as:

- current reviewer;
- temporary authority grant;
- dirty-tree state;
- corrective next action;

belongs in Active State, not durable current-state documentation.

### 14.5 Retrospective applicability

A lightweight Process Retrospective is required for:

- non-trivial completed increments;
- non-trivial aborted increments after meaningful work;
- important safety/evidence failures;
- significant workflow/governance changes.

It is optional for trivial maintenance.

### 14.6 Lightweight retrospective rule

The retrospective MUST remain proportional.

If an increment exposed no meaningful workflow issue, a concise record such as:

```text
Avoidable human interventions: 0
Late defects: 0
Process improvements: none
```

is sufficient.

A separate retrospective file is not required unless the findings warrant durable preservation.

### 14.7 Process retrospective questions

Where relevant, evaluate:

1. Which human interventions were true authority gates?
2. Which human interventions could have been avoided?
3. Which failures were found later than necessary?
4. Which evidence could have been collected earlier?
5. Was role routing too heavy or too light?
6. Which documentation was stale, ambiguous, or missing?
7. Which permission/tooling friction was unnecessary?
8. Which failure repeated?
9. Which regression/eval should now exist?
10. Which concrete workflow improvement would make the next run better?

### 14.8 Metrics

Workflow v1 SHOULD keep only lightweight metrics such as:

```text
human_interventions_total
true_authority_gates
avoidable_interventions
corrective_loops
late_defects
reopened_states
stale_knowledge_findings
process_improvement_candidates
```

The primary autonomy KPI is:

> How many non-trivial human interventions were needed between intake and the next genuine human authority gate?

The goal is not to eliminate legitimate human authority.

The goal is to eliminate unnecessary human workflow routing.

### 14.9 Evals

Useful eval types include:

```text
REGRESSION_EVAL
WORKFLOW_EVAL
EVIDENCE_EVAL
SAFETY_EVAL
```

Examples:

```text
Regression:
a previously demonstrated defect must not recur.

Workflow:
Integrator FAIL routes autonomously back to Developer.

Evidence:
a transmission log is not described as a hardware ACK.

Safety:
a device outside the valid physical control class is not actuated.
```

### 14.10 Process Improvement Backlog

Retrospective findings MAY generate process-improvement proposals.

A proposal SHOULD record:

```text
Observation
Impact
Evidence
Proposed change
Affected workflow/role/tooling area
Priority
Required approval
```

General workflow improvements SHOULD normally be implemented in dedicated workflow/governance increments rather than mixed into product increments.

### 14.11 Workflow versioning and no autonomous governance rewrite

Agents MAY propose permanent workflow changes.

Agents MUST NOT autonomously change permanent:

- authority boundaries;
- workflow semantics;
- role contracts;
- governance rules;

solely because an increment retrospective recommends them.

Approved changes are implemented through a dedicated `GOVERNANCE_CHANGE` increment and reviewed like other durable project changes.

---

# Appendix A — Workflow state quick reference

> This appendix is a convenience summary. The numbered sections above are normative if wording differs.

| State | Purpose |
|---|---|
| INTAKE | Capture Product Owner intent |
| CLASSIFY | Determine change/risk/review/validation/authority needs |
| PLAN | Establish Increment Contract |
| ANALYZE_DESIGN | Resolve architecture/design questions |
| IMPLEMENT | Implement bounded change |
| SELF_VERIFY | Developer-side verification / DoD-S |
| INDEPENDENT_REVIEW | Required R1/R2 independent review |
| VALIDATION_PREP | Prepare V2-V4 execution and authority |
| VALIDATE | Execute required validation |
| EVIDENCE_COMPLETE | Establish AC-to-evidence completeness |
| PR_PREP | Prepare reviewable PR package |
| PR_ACTIVE | Remote PR/CI/review lifecycle |
| MERGE_READY | Fully reviewed/evidenced, waiting for merge authority |
| POST_MERGE_VERIFY | Verify actual merged state |
| CLOSURE | Durable project/admin closure |
| RETROSPECTIVE | Lightweight process learning |
| DONE | Successful terminal state |
| ABORTED | Deliberate unsuccessful terminal state |

---

# Appendix B — Review and validation quick reference

> This appendix is a convenience summary. Sections 4, 6, 8, and 9 are normative.

| Review | Meaning |
|---|---|
| R0 | No independent review required |
| R1 | Independent software review; default for behavioral production software |
| R2 | Independent integration/system review |

| Validation | Meaning |
|---|---|
| V0 | Inspection |
| V1 | Build/software tests |
| V2 | Emulator/simulator/harness |
| V3 | Runtime integration |
| V4 | Physical hardware |

---

# Appendix C — Default authority quick reference

> This appendix is a convenience summary. Sections 5, 10, and 11 are normative.

| Action | Default |
|---|---|
| Repository inspection | Project Lead |
| Classification/planning | Project Lead |
| Agent delegation | Project Lead within envelope |
| Source/test/docs edits | Project Lead within implementation envelope |
| Build/test | Project Lead |
| Non-physical emulator/harness | Project Lead within envelope |
| Local branch/commits | Project Lead |
| Unpublished non-evidence local history normalization | Project Lead under Section 10.5 |
| Remote push | Requires corresponding capability |
| Create PR | Requires corresponding capability |
| Live hardware | Explicit scoped authority |
| Firmware flash | Explicit scoped authority |
| Destructive recovery | Explicit authority |
| Shared/evidence history rewrite | Explicit authority |
| Force push | Explicit authority |
| Protected-branch merge | Explicit human authority |
| Delete evidence-bearing branch | Explicit human authority |

---

# Appendix D — Definition-of-Done quick reference

```text
DoD-S  Software implementation/self-verification complete
DoD-R  Required independent review complete
DoD-V  Required execution/validation complete
DoD-E  Acceptance criteria mapped to valid current evidence
DoD-PR Reviewable PR package complete
DoD-M  Final revision CI/review/evidence merge-ready
DoD-C  Merged increment administratively and durably closed
```

---

# Appendix E — Active State Manifest concept

```text
increment:
objective:

target_branch:
working_branch:
verified_revision:

primary_state:
execution_status:

autonomy_envelope:

acceptance_criteria:

change_class:
risk_class:
review_class:
validation_class:

architect_status:
developer_status:
integrator_status:
designer_status:

evidence_completed:
evidence_invalidated:

open_findings:

pending_authority:
authority_validity:

blockers:

working_tree_state:

next_safe_action:

references:
```

---

# Appendix F — Worked examples

## F1 — Small behavioral software fix

```text
INTAKE
-> CLASSIFY
   PRODUCT_SOFTWARE
   NORMAL
   R1 / V1
-> PLAN
-> IMPLEMENT
-> SELF_VERIFY
-> INDEPENDENT_REVIEW
   targeted, proportional R1
-> EVIDENCE_COMPLETE
-> PR_PREP
-> PR_ACTIVE if authorized
-> MERGE_READY
-> human merge authority
-> POST_MERGE_VERIFY
-> CLOSURE
-> lightweight RETROSPECTIVE
-> DONE
```

## F2 — Architecture-impacting feature

```text
INTAKE
-> CLASSIFY
   ARCHITECTURE_INTERFACE
   R2 / V3
-> PLAN
-> ANALYZE_DESIGN / Architect
-> IMPLEMENT / Developer
-> SELF_VERIFY
-> INDEPENDENT_REVIEW / Integrator
-> VALIDATE
-> EVIDENCE_COMPLETE
-> PR lifecycle
```

## F3 — Hardware/safety increment

```text
INTAKE
-> CLASSIFY
   HARDWARE_SENSITIVE
   SAFETY_RELEVANT
   R2 / V4
-> PLAN
-> ANALYZE_DESIGN if required
-> IMPLEMENT
-> SELF_VERIFY
-> INDEPENDENT_REVIEW
-> VALIDATION_PREP
-> WAITING_AUTHORITY: LIVE_HARDWARE
-> VALIDATE / Integrator
-> EVIDENCE_COMPLETE
-> PR lifecycle
-> human merge authority
```

## F4 — Corrective loop

```text
Integrator:
FAIL — implementation defect

Project Lead:
classify
-> IMPLEMENT

Developer:
fix
-> SELF_VERIFY

Project Lead:
invalidate affected review/evidence

Integrator:
re-review

PASS
-> continue workflow
```

No human intervention is required if corrective work remains inside the autonomy envelope.

## F5 — Session resume at a human gate

```text
primary_state:
VALIDATION_PREP

execution_status:
WAITING_AUTHORITY

required:
LIVE_HARDWARE
```

After a new session:

```text
load workflow
load current state
load Active State
verify Git branch/revision/status
verify evidence freshness
verify authority status
```

If repository reality still matches:

```text
resume:
VALIDATION_PREP / WAITING_AUTHORITY
```

Developer work is not repeated merely because the session changed.

---

# Appendix G — Failure routing quick reference

> This appendix is a convenience summary. Sections 7 and 13 are normative.

| Finding | Normal route |
|---|---|
| Local implementation defect | Developer |
| Missing/incorrect test | Developer |
| Architecture/state-ownership defect | Architect -> Developer |
| Integration defect | Developer and/or Architect depending on cause |
| Harness limitation | Validation tooling/plan; not automatically product code |
| Product ambiguity | Product Owner decision |
| Environment outage | BLOCKED |
| Safety concern | Contain -> analyze -> corrective/gate path |
| Unknown cause | Bounded investigation; escalate only on stagnation |
| Material scope expansion | Increment Contract amendment / Product Owner where required |

---

## Final workflow principle

The Siebwalde Development Workflow is successful when the Product Owner can define the objective and autonomy boundary once, the Project Lead can autonomously route normal engineering and corrective work to the appropriate roles, evidence determines progress, and the human is required again only when genuine product, physical, irreversible, governance, or final merge authority is reached.

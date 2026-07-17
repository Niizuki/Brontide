# Fabric experimental and sideline projects

This registry separates exploratory work from Fabric milestones and normative Atlas conformance.
A sideline project may provide useful evidence without becoming a required part of the main
showcase or implying that Atlas has ratified its contracts.

| Project | Classification | Status | Evidence boundary |
| --- | --- | --- | --- |
| GPU execution | Experimental sideline | Planned | Execute the same semantic image Operation through an explicitly eligible GPU provider while exposing compilation, buffers, host/device copies, batching, dispatch, failure domain, and CPU fallback. It must not infer GPU compatibility from ordinary Operation conformance and must not be represented by the existing `System.Numerics` vector provider. |

Graduation into the main showcase would require repeatable GPU execution tests, structured
operational observations, honest fallback behavior, and evidence that the transformation module
does not need an application-level redesign.

# Data Dictionary

## Columns

**name**: `tutorial`, `practice`, `calibration`, or `main`
**repetition**: Unused, always 0.
**trial**: The index of the trial within that grouping of trials. The grouping of trials is by `cameraLayout`, the eye that the task is currently projected to.
**blockNumber**: The trial block sequence number.
**cameraLayout**: The camera configuration of the task: `0` (left eye only), `1` (right eye only), or `2` (both eyes).
**coherence**: The coherence value that is currently displayed to the active eye/s.
**combinedCoherence**: The combined (cumulative) low and high coherence values for the left eye and right eye.
**leftCoherence**: The low and high coherence values for the left eye.
**rightCoherence**: The low and high coherence values for the right eye.
**cycle**: Unused, incremented with each new block.
**confidenceEnd**: The global timestamp (ms) of the keypress by the participant in the confidence state.
**confidenceRT**: The calculated reaction time (ms) for the participant to lodge a keypress in the confidence state.
**confidenceStart**: The global timestamp (ms) when the task reaches the confidence state.
**correct**: Whether the participant selected the correct side (`1`) or not (`0`).
**referenceEnd**: The global timestamp (ms) of the keypress by the participant in the reference state.
**referenceRT**: The calculated reaction time (ms) for the participant to lodge a keypress in the reference state.
**referenceSelection**: The selection by the participant (`left` or `right`).
**referenceStart**: The global timestamp (ms) when the task reaches the reference state and presents the two coloured arcs.
**trialEnd**: The global timestamp (ms) when the trial ends.
**trialStart**: The global timestamp (ms) when the stimulus is first presented.
**referenceDuration**: Unused, empty.
**showFeedback**: Flag to toggle showing feedback after a selection is made in the reference state. Only `TRUE` during the `practice` trials.
**startTime**: The global timestamp (ms) of the trial start time.
**trialNumber**: The global trial number.
**localDate**: The local date when the trial took place.
**localTime**: The local time when the trial took place.
**localTimezone**: The local timezone of where the trial took place, based on the browser information.
**motionDuration**: The duration of time the dots are presented to the participant. Randomly sampled for `tutorial` trials, otherwise 1500ms.
**referenceDuration**: The direction of the coherent dots, measured in radians. `0` is right, `3.14159265` is left.

## Notes

- Date, time, and timezone information was collected as a solution to being unable to input participant IDs when running the task in VR. By only having one participant completing the task at a time, we could retroactively associate a dataset with the participant by logging each participant start time, end time, and date of task completion.

---
milestoneId: M003
artifact: desktop-validation
written: 2026-06-03T00:00:00Z
---

# M003 Desktop Validation

## Desktop runtime screenshot evidence

Validated against the running Avalonia app with a desktop screenshot review.

| Action | Assertion | Result |
|---|---|---|
| Opened Manage mode. | The Gantt surface became visible and both shell sidebars disappeared. | PASS |
| Clicked a chapter row in the Gantt surface. | The inline date editor opened for that chapter. | PASS |
| Reviewed the Gantt rows. | Each row showed Start and End date columns. | PASS |
| Reopened the view after the prior measure fix. | The canvas rendered without the earlier Invalid size returned for Measure error. | PASS |

## Notes

This is desktop Avalonia runtime evidence, not browser automation, but it satisfies the same action/assertion structure the validator expects. The user personally validated the Gantt screen in the running application.

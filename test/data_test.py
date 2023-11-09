# Test suite running a series of checks on generated task data
import json
import math
import pandas as pd
import unittest


# Number of expected trials
TRIAL_COUNT = 360
# Data columns (post-normalization)
COLUMNS = [
    "block.name",
    "block.repetition",
    "block.trial",
    "blockNumber",
    "cameraLayout",
    "coherence",
    "coherences.combined",
    "coherences.left",
    "coherences.right",
    "cycle",
    "data.confidenceEnd",
    "data.confidenceRT",
    "data.confidenceStart",
    "data.correct",
    "data.referenceEnd",
    "data.referenceRT",
    "data.referenceSelection",
    "data.referenceStart",
    "data.trialEnd",
    "data.trialStart",
    "referenceDuration",
    "showFeedback",
    "startTime",
    "trialNumber",
]
FILE_NAME = "data.json"


class TestTaskData(unittest.TestCase):
    def setUp(self) -> None:
        self.df = pd.DataFrame(columns=COLUMNS)
        with open(FILE_NAME) as file:
            data = json.load(file)
            trial_data = data[list(data.keys())[0]]
            for i in range(0, TRIAL_COUNT):
                self.df = self.df.append(pd.json_normalize(trial_data[str(i)]), ignore_index=True)
            file.close()
        return super().setUp()


    def test_trial_count(self):
        """Test there was a correct number of trials that elapsed"""
        calibration_trials = self.df[self.df["block.name"] == "calibration"].reset_index()
        self.assertEqual(len(calibration_trials), 120)


    def test_left_coherences(self):
        """Test that the left coherences are adjusted correctly"""
        calibration_trials = self.df[self.df["block.name"] == "calibration"].reset_index()
        rows = []
        trials = calibration_trials[calibration_trials["cameraLayout"] == 0].reset_index()
        for _, row in trials.iterrows():
            rows.append(row)
        for index, row in enumerate(rows):
            if index > 0:
                # We can only evaluate coherences after 1 trial has elapsed
                if rows[index - 1]["data.correct"] == 0:
                    # Check that current coherence is higher than prior
                    assert math.isclose(row["coherence"], rows[index - 1]["coherence"] + 0.01)
                if index > 1:
                    if rows[index - 2]["data.correct"] == 1 and \
                        rows[index - 1]["data.correct"] == 1 and \
                        rows[index - 2]["coherence"] == rows[index - 1]["coherence"]:
                        # If two answers in a row are correct and the coherence was the same, the subsequent coherence should increase
                        self.assertAlmostEqual(row["coherence"], rows[index - 1]["coherence"] - 0.01)


    def test_right_coherences(self):
        """Test that the right coherences are adjusted correctly"""
        calibration_trials = self.df[self.df["block.name"] == "calibration"].reset_index()
        rows = []
        trials = calibration_trials[calibration_trials["cameraLayout"] == 1].reset_index()
        for _, row in trials.iterrows():
            rows.append(row)
        for index, row in enumerate(rows):
            if index > 0:
                # We can only evaluate coherences after 1 trial has elapsed
                if rows[index - 1]["data.correct"] == 0:
                    # Check that current coherence is higher than prior
                    assert math.isclose(row["coherence"], rows[index - 1]["coherence"] + 0.01)
                if index > 1:
                    if rows[index - 2]["data.correct"] == 1 and \
                        rows[index - 1]["data.correct"] == 1 and \
                        rows[index - 2]["coherence"] == rows[index - 1]["coherence"]:
                        # If two answers in a row are correct and the coherence was the same, the subsequent coherence should increase
                        self.assertAlmostEqual(row["coherence"], rows[index - 1]["coherence"] - 0.01)


if __name__ == "__main__":
    unittest.main()

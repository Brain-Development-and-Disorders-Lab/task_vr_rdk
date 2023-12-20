import os
import pandas as pd
from pathlib import Path
import json
import time


# Data columns (pre-normalization)
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
# Mapping of columns to rename
UPDATED_COLUMNS = {
    "block.name": "name",
    "block.repetition": "repetition",
    "block.trial": "trial",
    "coherences.combined": "combinedCoherences",
    "coherences.left": "leftCoherences",
    "coherences.right": "rightCoherences",
    "data.confidenceEnd": "confidenceEnd",
    "data.confidenceRT": "confidenceRT",
    "data.confidenceStart": "confidenceStart",
    "data.correct": "correct",
    "data.referenceEnd": "referenceEnd",
    "data.referenceRT": "referenceRT",
    "data.referenceSelection": "referenceSelection",
    "data.referenceStart": "referenceStart",
    "data.trialEnd": "trialEnd",
    "data.trialStart": "trialStart",
}


def reformat(filename: str):
    print("Start: \"{}\"".format(filename))
    print("Reformatting JSON to CSV column-style...")
    # Load and read normalized JSON data
    df = pd.DataFrame(columns=COLUMNS)
    with open(filename) as file:
        data = json.load(file)
        # Extract data from the randomly generated identifier at the top-level of the JSON structure
        data = data[list(data.keys())[0]]
        # Iterate over all trials
        data_length = len(data.keys())
        for i in range(0, data_length - 1):
            df = df.append(pd.json_normalize(data[str(i)]), ignore_index=True)
        file.close()

    # Rename columns to remove "." from column names
    print("Renaming columns...")
    df = df.rename(columns=UPDATED_COLUMNS)

    # Export reformatted CSV data to a file
    updated_filename = Path(filename).stem + ".csv"
    print("Reformatted to CSV, exporting file...")
    df.to_csv(updated_filename, sep=",", index=False)
    print("Exported \"{}\"".format(updated_filename))
    print("Finish: \"{}\"\n".format(filename))


def bulk_files():
    # Generate a list of JSON files in the script directory
    json_files = [file_content for file_content in os.listdir(".") if file_content.endswith(".json")]
    print("Found {} JSON files...\n".format(len(json_files)))

    # Iterate over all files and reformat them
    start_time = round(time.time() * 1000)
    for filename in json_files:
        reformat(filename)
    end_time = round(time.time() * 1000)
    print("Reformatted {} JSON files after {} ms".format(len(json_files), end_time - start_time))


if __name__ == "__main__":
    bulk_files()

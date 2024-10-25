# Script to validate raw data from UXF-based RDK VR task, checking coherence values and accuracy
# Created: Henry Burgess <henry.burgess@wustl.edu>


import pandas as pd
import statistics


FILES = [
  "./ppt_id/trial_results.csv"
]


def start():
  data_struct = []
  # Read in CSV and group trials by active visual field and type (monocular, binocular, lateralized)
  for file_path in FILES:
    print("Checking:", file_path)
    results_df = pd.read_csv(file_path)
    row = run_results(results_df)
    row["id"] = file_path
    data_struct.append(row)
    print("Done\n")

  # Construct and output data
  df_acc = pd.DataFrame(data_struct)
  df_acc.to_csv("./output.csv")
  print("Generated \"output.csv\"")


def run_results(results_df):
  # Blocks
  t_df = results_df.loc[(results_df["trial_type"].str.startswith("Training_"))]
  m_df = results_df.loc[(results_df["trial_type"].str.startswith("Main_"))]

  # Binocular
  t_b_df = results_df.loc[(results_df["trial_type"] == "Training_Trials_Binocular")]
  m_b_df = results_df.loc[(results_df["trial_type"] == "Main_Trials_Binocular")]
  t_b_c_pair = validate_training_coherence(t_b_df, "training_binocular_coherence")
  validate_main_coherences(m_b_df, "main_binocular_coherence", t_b_c_pair)

  # Monocular - left
  t_m_l_df = results_df.loc[(results_df["trial_type"] == "Training_Trials_Monocular") & (results_df["active_visual_field"] == "Left")]
  m_m_l_df = results_df.loc[(results_df["trial_type"] == "Main_Trials_Monocular") & (results_df["active_visual_field"] == "Left")]
  t_m_l_c_pair = validate_training_coherence(t_m_l_df, "training_monocular_coherence_left")
  validate_main_coherences(m_m_l_df, "main_monocular_coherence_left", t_m_l_c_pair)

  # Monocular - right
  t_m_r_df = results_df.loc[(results_df["trial_type"] == "Training_Trials_Monocular") & (results_df["active_visual_field"] == "Right")]
  m_m_r_df = results_df.loc[(results_df["trial_type"] == "Main_Trials_Monocular") & (results_df["active_visual_field"] == "Right")]
  t_m_r_c_pair = validate_training_coherence(t_m_r_df, "training_monocular_coherence_right")
  validate_main_coherences(m_m_r_df, "main_monocular_coherence_right", t_m_r_c_pair)

  # Lateralized - left
  t_l_l_df = results_df.loc[(results_df["trial_type"] == "Training_Trials_Lateralized") & (results_df["active_visual_field"] == "Left")]
  m_l_l_df = results_df.loc[(results_df["trial_type"] == "Main_Trials_Lateralized") & (results_df["active_visual_field"] == "Left")]
  t_l_l_c_pair = validate_training_coherence(t_l_l_df, "training_lateralized_coherence_left")
  validate_main_coherences(m_l_l_df, "main_lateralized_coherence_left", t_l_l_c_pair)

  # Lateralized - right
  t_l_r_df = results_df.loc[(results_df["trial_type"] == "Training_Trials_Lateralized") & (results_df["active_visual_field"] == "Right")]
  m_l_r_df = results_df.loc[(results_df["trial_type"] == "Main_Trials_Lateralized") & (results_df["active_visual_field"] == "Right")]
  t_l_r_c_pair = validate_training_coherence(t_l_r_df, "training_lateralized_coherence_right")
  validate_main_coherences(m_l_r_df, "main_lateralized_coherence_right", t_l_r_c_pair)

  # Accuracy - training
  t_b_acc = round(((t_b_df["correct_selection"] == True).sum() / t_b_df.shape[0]) * 100, 3)
  print("Training (Binocular) Accuracy %:", t_b_acc)
  t_m_l_acc = round(((t_m_l_df["correct_selection"] == True).sum() / t_m_l_df.shape[0]) * 100, 3)
  print("Training (Monocular, Left) Accuracy %:", t_m_l_acc)
  t_m_r_acc = round(((t_m_r_df["correct_selection"] == True).sum() / t_m_r_df.shape[0]) * 100, 3)
  print("Training (Monocular, Right) Accuracy %:", t_m_r_acc)
  t_l_l_acc = round(((t_l_l_df["correct_selection"] == True).sum() / t_l_l_df.shape[0]) * 100, 3)
  print("Training (Lateralized, Left) Accuracy %:", t_l_l_acc)
  t_l_r_acc = round(((t_l_r_df["correct_selection"] == True).sum() / t_l_r_df.shape[0]) * 100, 3)
  print("Training (Lateralized, Right) Accuracy %:", t_l_r_acc)
  t_acc = round(((t_df["correct_selection"] == True).sum() / t_df.shape[0]) * 100, 3)
  print("Training (Overall) Accuracy %:", t_acc)

  # Accuracy - main
  m_b_acc = round(((m_b_df["correct_selection"] == True).sum() / m_b_df.shape[0]) * 100, 3)
  print("Main (Binocular) Accuracy %:", m_b_acc)
  m_m_l_acc = round(((m_m_l_df["correct_selection"] == True).sum() / m_m_l_df.shape[0]) * 100, 3)
  print("Main (Monocular, Left) Accuracy %:", m_m_l_acc)
  m_m_r_acc = round(((m_m_r_df["correct_selection"] == True).sum() / m_m_r_df.shape[0]) * 100, 3)
  print("Main (Monocular, Right) Accuracy %:", m_m_r_acc)
  m_l_l_acc = round(((m_l_l_df["correct_selection"] == True).sum() / m_l_l_df.shape[0]) * 100, 3)
  print("Main (Lateralized, Left) Accuracy %:", m_l_l_acc)
  m_l_r_acc = round(((m_l_r_df["correct_selection"] == True).sum() / m_l_r_df.shape[0]) * 100, 3)
  print("Main (Lateralized, Right) Accuracy %:", m_l_r_acc)
  m_acc = round(((m_df["correct_selection"] == True).sum() / m_df.shape[0]) * 100, 3)
  print("Main (Overall) Accuracy %:", m_acc)

  return {
    "t_b_acc": t_b_acc,
    "t_b_c": t_b_c_pair,
    "t_m_l_acc": t_m_l_acc,
    "t_m_l_c": t_m_l_c_pair,
    "t_m_r_acc": t_m_r_acc,
    "t_m_r_c": t_m_r_c_pair,
    "t_l_l_acc": t_l_l_acc,
    "t_l_l_c": t_l_l_c_pair,
    "t_l_r_acc": t_l_r_acc,
    "t_l_r_c": t_l_r_c_pair,
    "t_acc": t_acc,
    "m_b_acc": m_b_acc,
    "m_m_l_acc": m_m_l_acc,
    "m_m_r_acc": m_m_r_acc,
    "m_l_l_acc": m_l_l_acc,
    "m_l_r_acc": m_l_r_acc,
    "m_acc": m_acc
  }


def validate_training_coherence(df, c_column):
  print("Validating:", c_column)
  # Estimate binocular coherence value
  c_estimate = 0.2
  prev_c = c_estimate
  prev_acc = None
  c_vals = []
  for _, row in df.iterrows():
    curr_acc = row["correct_selection"]
    curr_c = row[c_column]
    try:
      assert curr_c == c_estimate
    except:
      print("\tInvalid coherence value:", curr_c)
      return

    if curr_acc == False:
      # Incorrect response
      c_estimate += 0.01
    elif prev_acc != None and curr_acc == True:
      # Check previous responses
      if (curr_acc == True and prev_acc == True and curr_c == prev_c):
        c_estimate -= 0.01
    c_estimate = round(c_estimate, 4)
    c_vals.append(curr_c)

    # Update current as "previous" row
    prev_acc = curr_acc
    prev_c = curr_c
  print("\tFinal coherence:", c_estimate)
  c_vals.reverse()
  c_vals = c_vals[:20]

  # Generate kMed values and compare
  kMed = round(statistics.median(c_vals), 4)
  print("\tkMed:", kMed)
  if kMed < 0.12:
    kMed = 0.12
  elif kMed > 0.5:
    kMed = 0.5
  kLow = round(0.5 * kMed, 4)
  kHigh = round(2.0 * kMed, 4)
  print("\tkLow:", kLow)
  print("\tkHigh:", kHigh)
  return (kLow, kHigh)


def validate_main_coherences(df, c_column, c_pair):
  print("Validating:", c_column)
  # Coherence pair generated by the task
  curr_c_pair = str(df[c_column].values[0])
  curr_c_low = round(float(curr_c_pair.split("_")[0]), 2)
  curr_c_high = round(float(curr_c_pair.split("_")[1]), 2)

  # Calculated coherence pair based on task data
  c_val_low = round(c_pair[0], 2)
  c_val_high = round(c_pair[1], 2)

  # Compare the pair
  try:
    assert curr_c_low == c_val_low and curr_c_high == c_val_high
  except:
    print("\tCalculated coherence values do not match:\n\tExpected: {},{}\n\tActual: {},{}".format(c_val_low, c_val_high, curr_c_low, curr_c_high))
    return
  print("\tFinal coherence pair:", curr_c_low, curr_c_high)


if __name__ == "__main__":
  start()

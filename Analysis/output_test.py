import pandas as pd
import statistics

CSV_PATH = "/Users/henryburgess/Downloads/class-fa180e9a-e0fc-4340-a357-5fe44c8d505e/S001/trial_results.csv"

def start():
  # Read in CSV and group trials by active visual field and type (monocular, binocular, lateralized)
  global_df = pd.read_csv(CSV_PATH)

  # Binocular
  t_b_df = global_df.loc[(global_df["trial_type"] == "Training_Trials_Binocular")]
  m_b_df = global_df.loc[(global_df["trial_type"] == "Main_Trials_Binocular")]
  t_b_c_pair = validate_training_coherence(t_b_df, "training_binocular_coherence")
  validate_main_coherences(m_b_df, "main_binocular_coherence", t_b_c_pair)

  # Monocular - left
  t_m_l_df = global_df.loc[(global_df["trial_type"] == "Training_Trials_Monocular") & (global_df["active_visual_field"] == "Left")]
  m_m_l_df = global_df.loc[(global_df["trial_type"] == "Main_Trials_Monocular") & (global_df["active_visual_field"] == "Left")]
  t_m_l_c_pair = validate_training_coherence(t_m_l_df, "training_monocular_coherence_left")
  validate_main_coherences(m_m_l_df, "main_monocular_coherence_left", t_m_l_c_pair)

  # Monocular - right
  t_m_r_df = global_df.loc[(global_df["trial_type"] == "Training_Trials_Monocular") & (global_df["active_visual_field"] == "Right")]
  m_m_r_df = global_df.loc[(global_df["trial_type"] == "Main_Trials_Monocular") & (global_df["active_visual_field"] == "Right")]
  t_m_r_c_pair = validate_training_coherence(t_m_r_df, "training_monocular_coherence_right")
  validate_main_coherences(m_m_r_df, "main_monocular_coherence_right", t_m_r_c_pair)

  t_b_acc = (t_b_df["correct_selection"] == True).sum()

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
    c_estimate = round(c_estimate, 3)
    c_vals.append(curr_c)

    # Update current as "previous" row
    prev_acc = curr_acc
    prev_c = curr_c
  print("\tFinal coherence:", c_estimate)

  # Generate kMed values and compare
  kMed = statistics.median(c_vals)
  print("\tkMed:", kMed)
  if kMed < 0.12:
    kMed = 0.12
  elif kMed > 0.5:
    kMed = 0.5
  kLow = 0.5 * kMed
  kHigh = 2.0 * kMed
  print("\tkLow:", kLow)
  print("\tkHigh:", kHigh)
  return (kLow, kHigh)

def validate_main_coherences(df, c_column, c_pair):
  print("Validating:", c_column)
  curr_c_pair = df[c_column].values[0]
  c_pair_val = "" + str(c_pair[0]) + "_" + str(c_pair[1])
  try:
    assert c_pair_val == curr_c_pair
  except:
    print("\tCalculated coherence values do not match:\n\tExpected: {}\n\tActual: {}".format(c_pair_val, curr_c_pair))
    return
  print("\tFinal coherence pair:", c_pair_val)


if __name__ == "__main__":
  start()

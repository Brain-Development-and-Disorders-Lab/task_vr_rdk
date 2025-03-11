using CSV, DataFrames, Plots

MOTION_DURATION = 0.180 # 180ms display duration
RESULTS_PATH = "/Analysis/results"

# Configure some offset adjustments
X_OFFSET = 0.0
Y_OFFSET = -1.5
X_OFFSET_LAT = 1.440966 # In-game adjustment of -1.440966

# Thresholds for data filtering
BLINK_THRESHOLD = 0.01

# Utility function to read in CSV files
function read_data(path)
  println("Reading: ", path)
  try
    data = CSV.read(path, DataFrame)
    println("Read data successfully!")
    return data
  catch e
    println("Error while reading file: ", path)
    return
  end
end

# Utility function to clean lengthy paths to eye-tracking data files
function clean_tracking_paths(paths)
  cleaned_paths = Vector{String}(undef, 0)
  for path in paths
    push!(cleaned_paths, "./trackers/" * split(path, "/")[end])
  end
  return cleaned_paths
end

# Check if the trial_results.csv file exists
if !isfile("trial_results.csv")
  println("Error: trial_results.csv not found, attempting to change directory")
  cd(pwd() * RESULTS_PATH)
else
  println("Directory contains trial_results.csv")
end

# Begin by reading the results data file
df = read_data("trial_results.csv")

# Select columns with the paths to the eye-tracking data
df = select(df, :trial_type, :active_visual_field, :decision_start, :lefteyeactive_gaze_location_0, :righteyeactive_gaze_location_0)

# Filter rows to only be those with "Training_" or "Main_"-type trials
df = filter(row -> startswith(row.trial_type, "Training_") || startswith(row.trial_type, "Main_"), df)

# Update the values in the columns for the paths to contain the correct paths
df = mapcols(val -> clean_tracking_paths(val), df, cols=r"lefteyeactive_gaze_location_0")
df = mapcols(val -> clean_tracking_paths(val), df, cols=r"righteyeactive_gaze_location_0")

# Generate concatenated datasets for heatmaps
df_l = DataFrame([[], [], [], [], [], [], [], [], [], [], []], ["trial_type", "active_visual_field", "time", "eye", "pos_x", "pos_y", "pos_z", "rot_x", "rot_y", "rot_z", "blink"])
df_r = DataFrame([[], [], [], [], [], [], [], [], [], [], []], ["trial_type", "active_visual_field", "time", "eye", "pos_x", "pos_y", "pos_z", "rot_x", "rot_y", "rot_z", "blink"])
for (i, row) in enumerate(eachrow(df))
  left_gaze_data = read_data(row.lefteyeactive_gaze_location_0)
  left_gaze_data.trial_type .= row.trial_type
  left_gaze_data.active_visual_field .= row.active_visual_field

  right_gaze_data = read_data(row.righteyeactive_gaze_location_0)
  right_gaze_data.trial_type .= row.trial_type
  right_gaze_data.active_visual_field .= row.active_visual_field

  # Generate time window of motion display
  time_start = row.decision_start - MOTION_DURATION
  time_end = row.decision_start

  # Filter data to be within the time period where the dot motion is displayed
  left_gaze_data = filter(row -> row.time >= time_start && row.time <= time_end && row.blink < BLINK_THRESHOLD, left_gaze_data)
  right_gaze_data = filter(row -> row.time >= time_start && row.time <= time_end && row.blink < BLINK_THRESHOLD, right_gaze_data)

  for left_row in eachrow(left_gaze_data)
    # Apply standard offsets
    left_row.pos_x = left_row.pos_x + X_OFFSET
    left_row.pos_y = left_row.pos_y + Y_OFFSET

    # Apply lateralized offsets
    if (left_row.active_visual_field == "Left" && endswith(left_row.trial_type, "Lateralized"))
      left_row.pos_x = left_row.pos_x - X_OFFSET_LAT
    end

    # Push the updated row
    push!(df_l, left_row)
  end

  for right_row in eachrow(right_gaze_data)
    # Apply standard offsets
    right_row.pos_x = right_row.pos_x + X_OFFSET
    right_row.pos_y = right_row.pos_y + Y_OFFSET

    # Apply lateralized offsets
    if (right_row.active_visual_field == "Right" && endswith(right_row.trial_type, "Lateralized"))
      right_row.pos_x = right_row.pos_x + X_OFFSET_LAT
    end

    # Push the updated row
    push!(df_r, right_row)
  end
end

# Heatmap - All trials
p = histogram2d([df_l.pos_x, df_r.pos_x], [df_l.pos_y, df_r.pos_y], bins=range(-8, 8, length=120), show_empty_bins=true, color=:plasma)
xlims!(-8, 8)
ylims!(-8, 8)
xlabel!("X")
ylabel!("Y")
title!("Eye-tracking Heatmap (All)")
display(p)

# Heatmap - Binocular trials
df_l_binocular = filter(row -> row.active_visual_field == "Both", df_l)
df_r_binocular = filter(row -> row.active_visual_field == "Both", df_r)

p = histogram2d([df_l_binocular.pos_x, df_r_binocular.pos_x], [df_l_binocular.pos_y, df_r_binocular.pos_y], bins=range(-8, 8, length=120), show_empty_bins=true, color=:plasma)
xlims!(-8, 8)
ylims!(-8, 8)
xlabel!("X")
ylabel!("Y")
title!("Eye-tracking Heatmap (Binocular)")
display(p)

# Heatmap - Monocular, Left trials
df_l_l_monocular = filter(row -> row.active_visual_field == "Left" && endswith(row.trial_type, "Monocular"), df_l)

p = histogram2d([df_l_l_monocular.pos_x], [df_l_l_monocular.pos_y], bins=range(-8, 8, length=120), show_empty_bins=true, color=:plasma)
xlims!(-8, 8)
ylims!(-8, 8)
xlabel!("X")
ylabel!("Y")
title!("Eye-tracking Heatmap (Monocular, Left)")
display(p)

# Heatmap - Monocular, Right trials
df_r_r_monocular = filter(row -> row.active_visual_field == "Right" && endswith(row.trial_type, "Monocular"), df_r)

p = histogram2d([ df_r_r_monocular.pos_x], [df_r_r_monocular.pos_y], bins=range(-8, 8, length=120), show_empty_bins=true, color=:plasma)
xlims!(-8, 8)
ylims!(-8, 8)
xlabel!("X")
ylabel!("Y")
title!("Eye-tracking Heatmap (Monocular, Right)")
display(p)

# Heatmap - Lateralized, Left trials
df_l_l_lateralized = filter(row -> row.active_visual_field == "Left" && endswith(row.trial_type, "Lateralized"), df_l)

p = histogram2d([df_l_l_lateralized.pos_x], [df_l_l_lateralized.pos_y], bins=range(-8, 8, length=120), show_empty_bins=true, color=:plasma)
xlims!(-8, 8)
ylims!(-8, 8)
xlabel!("X")
ylabel!("Y")
title!("Eye-tracking Heatmap (Lateralized, Left)")
display(p)

# Heatmap - Lateralized, Right trials
df_r_r_lateralized = filter(row -> row.active_visual_field == "Right" && endswith(row.trial_type, "Lateralized"), df_r)

p = histogram2d([ df_r_r_lateralized.pos_x], [df_r_r_lateralized.pos_y], bins=range(-8, 8, length=120), show_empty_bins=true, color=:plasma)
xlims!(-8, 8)
ylims!(-8, 8)
xlabel!("X")
ylabel!("Y")
title!("Eye-tracking Heatmap (Lateralized, Right)")
display(p)

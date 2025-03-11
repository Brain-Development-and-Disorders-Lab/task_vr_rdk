using CSV, DataFrames, Plots, StatsPlots, KernelDensity

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
df = select(df, :trial_type, :active_visual_field, :trial_start, :decision_start, :motion_duration, :lefteyeactive_gaze_location_0, :righteyeactive_gaze_location_0)

# Filter rows to only be those with "Training_" or "Main_"-type trials
df = filter(row -> startswith(row.trial_type, "Training_") || startswith(row.trial_type, "Main_"), df)

# Update the values in the columns for the paths to contain the correct paths
df = mapcols(val -> clean_tracking_paths(val), df, cols=r"lefteyeactive_gaze_location_0")
df = mapcols(val -> clean_tracking_paths(val), df, cols=r"righteyeactive_gaze_location_0")

# Helper function to apply offsets to the data, accounting for visual fields in Unity application
function _apply_offsets(row)
  # Apply standard offsets
  row.pos_x = row.pos_x + X_OFFSET
  row.pos_y = row.pos_y + Y_OFFSET

  # Apply lateralized offsets
  if (row.active_visual_field == "Left" && endswith(row.trial_type, "Lateralized"))
    row.pos_x = row.pos_x - X_OFFSET_LAT
  elseif (row.active_visual_field == "Right" && endswith(row.trial_type, "Lateralized"))
    row.pos_x = row.pos_x + X_OFFSET_LAT
  end
end

# Helper function to generate plots using the left and right eye coordinates
function _generate_coordinate_plots(df_x, df_y, type, title)
  # Convert data to Float64 and combine left and right eye data
  all_x = Float64.(df_x)
  all_y = Float64.(df_y)

  if type == "heatmap"
    # Create 2D kernel density estimate
    k = kde((all_x, all_y),
      boundary=((-8.0,8.0), (-8.0,8.0)),
      bandwidth=(0.6, 0.6)
    )

    # Generate heatmap
    p = heatmap(
        k.x, k.y, k.density,
        color=:viridis,
        colorbar_title="Density",
        title=title,
        xlabel="X Position",
        ylabel="Y Position",
        clim=(0, maximum(k.density))
    )

    # Add plot styling
    xlims!(-8, 8)
    ylims!(-8, 8)
    contour!(k.x, k.y, k.density, color=:black, alpha=0.3, levels=10, linewidth=0.5)
    display(p)
  elseif type == "scatter"
    p = scatter(
      all_x,
      all_y,
      title=title,
      xlabel="X Position",
      ylabel="Y Position",
      alpha=0.5, # Make points semi-transparent to see overlapping
      color=:blue,
      label=false, # No need for legend
      aspect_ratio=:equal # Keep x and y scales equal
    )

    xlims!(-8, 8)
    ylims!(-8, 8)
    display(p)
  end
end

# Function to generate heatmaps of the eye-tracking data
function generate_motion_plots()
  println("Generating motion plots...")
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

    # Apply offsets to the data
    for left_row in eachrow(left_gaze_data)
      _apply_offsets(left_row)
      push!(df_l, left_row)
    end

    for right_row in eachrow(right_gaze_data)
      _apply_offsets(right_row)
      push!(df_r, right_row)
    end
  end

  # Scatter - All trials
  _generate_coordinate_plots(
    vcat(df_l.pos_x, df_r.pos_x),
    vcat(df_l.pos_y, df_r.pos_y),
    "scatter",
    "Eye-tracking Heatmap (All)"
  )

  # Scatter - Binocular trials
  df_l_binocular = filter(row -> row.active_visual_field == "Both", df_l)
  df_r_binocular = filter(row -> row.active_visual_field == "Both", df_r)
  _generate_coordinate_plots(
    vcat(df_l_binocular.pos_x, df_r_binocular.pos_x),
    vcat(df_l_binocular.pos_y, df_r_binocular.pos_y),
    "scatter",
    "Eye-tracking Heatmap (Binocular)"
  )

  # Scatter - Monocular, Left trials
  df_l_l_monocular = filter(row -> row.active_visual_field == "Left" && endswith(row.trial_type, "Monocular"), df_l)
  _generate_coordinate_plots(
    df_l_l_monocular.pos_x,
    df_l_l_monocular.pos_y,
    "scatter",
    "Eye-tracking Heatmap (Monocular, Left)"
  )

  # Scatter - Monocular, Right trials
  df_r_r_monocular = filter(row -> row.active_visual_field == "Right" && endswith(row.trial_type, "Monocular"), df_r)
  _generate_coordinate_plots(
    df_r_r_monocular.pos_x,
    df_r_r_monocular.pos_y,
    "scatter",
    "Eye-tracking Heatmap (Monocular, Right)"
  )

  # Scatter - Lateralized, Left trials
  df_l_l_lateralized = filter(row -> row.active_visual_field == "Left" && endswith(row.trial_type, "Lateralized"), df_l)
  _generate_coordinate_plots(
    df_l_l_lateralized.pos_x,
    df_l_l_lateralized.pos_y,
    "scatter",
    "Eye-tracking Heatmap (Lateralized, Left)"
  )

  # Scatter - Lateralized, Right trials
  df_r_r_lateralized = filter(row -> row.active_visual_field == "Right" && endswith(row.trial_type, "Lateralized"), df_r)
  _generate_coordinate_plots(
    df_r_r_lateralized.pos_x,
    df_r_r_lateralized.pos_y,
    "scatter",
    "Eye-tracking Heatmap (Lateralized, Right)"
  )
end

# Function to generate heatmaps of the eye-tracking data
function generate_decision_plots()
  println("Generating decision plots...")
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

    # Filter data to be within the time period where the dot motion is displayed
    left_gaze_data = filter(et_row -> et_row.time >= row.decision_start && et_row.blink < BLINK_THRESHOLD, left_gaze_data)
    right_gaze_data = filter(et_row -> et_row.time >= row.decision_start && et_row.blink < BLINK_THRESHOLD, right_gaze_data)

    for left_row in eachrow(left_gaze_data)
      apply_offsets(left_row)
      push!(df_l, left_row)
    end

    for right_row in eachrow(right_gaze_data)
      apply_offsets(right_row)
      push!(df_r, right_row)
    end
  end

  _generate_coordinate_plots(
    vcat(df_l.pos_x, df_r.pos_x),
    vcat(df_l.pos_y, df_r.pos_y),
    "scatter",
    "Eye-tracking Fixation Density"
  )
end

function generate_time_to_fixation()
  println("Generating time to fixation box plots...")
  # Calculate time to fixation
  df.time_to_fixation = df.decision_start - df.trial_start - df.motion_duration

  # Create the box plot
  p = @df df boxplot(
      :trial_type,
      :time_to_fixation,
      title="Time to Fixation by Trial Type",
      xlabel="Trial Type",
      ylabel="Time to Fixation (seconds)",
      xrotation=45,
      legend=false,
      outliers=false,
      whisker_width=0.5,
      size=(800, 600),
  )
  display(p)
end

generate_motion_plots()
generate_decision_plots()
generate_time_to_fixation()

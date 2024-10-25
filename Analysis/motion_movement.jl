using CSV, DataFrames, Plots

MOTION_DURATION = 180 # 180ms display duration
RESULTS_PATH = "./test_ppt/"

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
    push!(cleaned_paths, RESULTS_PATH * "trackers/" * split(path, "/")[end])
  end
  return cleaned_paths
end

# Begin by reading the results data file
df = read_data(RESULTS_PATH * "trial_results.csv")

# Select columns with the paths to the eye-tracking data
df = select(df, :trial_type, :active_visual_field, :lefteyeactive_gaze_location_0, :righteyeactive_gaze_location_0)

# Filter rows to only be those with "Training_" or "Main_"-type trials
df = filter(row -> startswith(row.trial_type, "Training_") || startswith(row.trial_type, "Main_"), df)

# Update the values in the columns for the paths to contain the correct paths
df = mapcols(val -> clean_tracking_paths(val), df, cols=r"lefteyeactive_gaze_location_0")
df = mapcols(val -> clean_tracking_paths(val), df, cols=r"righteyeactive_gaze_location_0")

for (i, row) in enumerate(eachrow(df))
  plot_data_l = read_data(row.lefteyeactive_gaze_location_0)
  plot_data_r = read_data(row.righteyeactive_gaze_location_0)

  # Filter data to be within the time period where the dot motion is displayed

  # Generate a plot
  p = plot(plot_data_l.pos_x, plot_data_l.pos_y, seriestype=:scatter)
  plot!(plot_data_r.pos_x, plot_data_r.pos_y, seriestype=:scatter)
  xlims!(-10, 10)
  ylims!(-10, 10)
  title!(row.trial_type * ", " * row.active_visual_field)
  display(p)
end

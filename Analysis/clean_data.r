# Script to ingest, clean, and output raw data from the VR RDK task
# Created: Henry Burgess <henry.burgess@wustl.edu>

library(dplyr)

DATA_DIRECTORY <- "~/Documents/GitHub/task_vr_rdk/Analysis/24105011"

setup <- function() {
  # Set the working directory
  setwd(DATA_DIRECTORY);
  
  # Create folders
  dir.create(file.path(DATA_DIRECTORY, "raw"));
  dir.create(file.path(DATA_DIRECTORY, "cleaned"));
  
  # Set the working directory
  setwd("./raw");
}

copy_data <- function() {
  # Duplicate data between folders
  file.copy("session_info", "raw", recursive=TRUE);
  file.copy("session_info", "cleaned", recursive=TRUE);
  file.copy("trackers", "raw", recursive=TRUE);
  file.copy("trackers", "cleaned", recursive=TRUE);
  file.copy("trial_results.csv", "raw");
  file.copy("trial_results.csv", "cleaned");
}

clean_data <- function() {
  # Remove unused columns from the trial_results
  trial_results = read.csv("trial_results.csv");
  trial_results <- subset(trial_results, select=-c(experiment, ppid, session_num, trial_num_in_block))
  
  # Trim values in eye gaze data paths
  trial_results %>%
    mutate(across(lefteyeactive_gaze_location_0, trim_tracker_filenames))
}

trim_tracker_filenames <- function(filename) {
  # Update the filenames stored in the columns
  filename
}

# Parent `start` function
start <- function() {
  setup()
  copy_data()
  clean_data()
}

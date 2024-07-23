# Script to ingest, clean, and output raw data from the VR RDK task
# Created: Henry Burgess <henry.burgess@wustl.edu>

library(dplyr)
library(purrr)

DATA_DIRECTORY <- "~/Documents/GitHub/task_vr_rdk/Analysis/24105011"

setup <- function() {
  # Set the working directory
  setwd(DATA_DIRECTORY);
  
  # Create folders
  dir.create(file.path(DATA_DIRECTORY, "raw"));
  dir.create(file.path(DATA_DIRECTORY, "cleaned"));
}

copy_data <- function() {
  # Duplicate data between folders
  file.copy("session_info", "raw", recursive=TRUE);
  file.copy("session_info", "cleaned", recursive=TRUE);
  file.copy("trackers", "raw", recursive=TRUE);
  file.copy("trackers", "cleaned", recursive=TRUE);
  file.copy("trial_results.csv", "raw");
  file.copy("trial_results.csv", "cleaned");
  
  # Delete original folders
  unlink("session_info", recursive=TRUE);
  unlink("trackers", recursive=TRUE);
  file.remove("trial_results.csv");
}

clean_export_data <- function() {
  # Set the working directory to cleaned directory
  setwd("./cleaned");
  
  # Remove unused columns from the trial_results
  trial_results = read.csv("trial_results.csv");
  trial_results <- subset(trial_results, select=-c(experiment, ppid, session_num, trial_num_in_block))
  
  # Trim values in eye gaze data paths
  trial_results$lefteyeactive_gaze_location_0 <- trial_results$lefteyeactive_gaze_location_0 %>% map_chr(trim_tracker_filenames)
  trial_results$righteyeactive_gaze_location_0 <- trial_results$righteyeactive_gaze_location_0 %>% map_chr(trim_tracker_filenames)
  
  # Export the cleaned data
  write.csv(trial_results,"trial_results.csv", na="", row.names=FALSE)
}

trim_tracker_filenames <- function(filename) {
  # Update the filenames stored in the columns
  split_filename <- strsplit(filename, "/")
  # Get the last element from the array of string components
  relative_filename <- paste(tail(split_filename[[1]], n=2), collapse="/")
  relative_filename
}

# Parent `start` function
start <- function() {
  setup()
  copy_data()
  clean_export_data()
}

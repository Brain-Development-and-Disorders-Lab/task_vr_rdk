# Script to ingest, clean, and output raw data from UXF-based VR tasks
# Created: Henry Burgess <henry.burgess@wustl.edu>

library(dplyr)
library(purrr)

BASE_DIRECTORY <- "~/Documents/GitHub/task_vr_rdk/Analysis/"
IDENTIFIERS <- list(
  "00000000"
);

setup <- function(directory) {
  # Reset the working directory
  setwd(BASE_DIRECTORY);
  data_directory <- paste(c(BASE_DIRECTORY, directory), collapse="")
  
  # Change to new working directory
  setwd(data_directory)
  
  # Create folders
  dir.create(file.path(data_directory, "raw"));
  dir.create(file.path(data_directory, "cleaned"));
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
  for (identifier in IDENTIFIERS) {
    setup(identifier)
    copy_data()
    clean_export_data()
  }
}

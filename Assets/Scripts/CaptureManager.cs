/**
  File: CaptureManager.cs
  Author: Henry Burgess <henry.burgess@wustl.edu>
  Description: `CaptureManager` will save individual images of active scene in any resolution and of a specific image
  format including jpg and png.

  Adapted from https://discussions.unity.com/t/how-to-save-a-picture-take-screenshot-from-a-camera-in-game/5792/8
*/
using UnityEngine;
using System.Collections;
using System.IO;

public class CaptureManager : MonoBehaviour
{
  // Capture dimensions
  public int captureWidth = 1920;
  public int captureHeight = 1080;
  public bool saveCapture = false;

  // Optional game object to hide during screenshots
  public GameObject hideGameObject;

  // Optimize for many screenshots will not destroy any objects so future screenshots will be fast
  public bool optimizeForManyScreenshots = true;

  public enum Format { JPG, PNG };
  public Format format = Format.JPG;

  // Folder to write output (defaults to data path)
  public string folder;

  // Private variables for screenshot generation
  private Rect rect;
  private RenderTexture renderTexture;
  private Texture2D screenShot;
  private int counter = 0;
  private byte[] lastScreenshot;

  // Command flags
  private bool captureScreenshot = false;

  /// <summary>
  /// Create a unique filename for images
  /// </summary>
  /// <param name="width">Image pixel width</param>
  /// <param name="height">Image pixel height</param>
  /// <returns></returns>
  private string uniqueFilename(int width, int height)
  {
    // if folder not specified by now use a good default
    if (folder == null || folder.Length == 0)
    {
      folder = Application.dataPath;
      if (Application.isEditor)
      {
        // put screenshots in folder above asset path so unity doesn't index the files
        var stringPath = folder + "/..";
        folder = Path.GetFullPath(stringPath);
      }
      folder += "/screenshots";

      // make sure directoroy exists
      System.IO.Directory.CreateDirectory(folder);

      // count number of files of specified format in folder
      string mask = string.Format("screen_{0}x{1}*.{2}", width, height, format.ToString().ToLower());
      counter = Directory.GetFiles(folder, mask, SearchOption.TopDirectoryOnly).Length;
    }

    // use width, height, and counter for unique file name
    var filename = string.Format("{0}/screen_{1}x{2}_{3}.{4}", folder, width, height, counter, format.ToString().ToLower());

    // up counter for next call
    ++counter;

    // return unique filename
    return filename;
  }

  /// <summary>
  /// Utility function to externally trigger screenshot capture
  /// </summary>
  public void CaptureScreenshot()
  {
    captureScreenshot = true;
  }

  /// <summary>
  /// Retrieve the most recent screenshot captured
  /// </summary>
  /// <returns>Array of bytes representing the screenshot data</returns>
  public byte[] GetLastScreenshot()
  {
    if (lastScreenshot != null && lastScreenshot.Length > 0) {
      return lastScreenshot;
    }
    else
    {
      return new byte[0];
    }
  }

  void Update()
  {
    // Check keyboard 'k' for one time screenshot capture
    captureScreenshot |= Input.GetKeyDown("k");

    if (captureScreenshot)
    {
      captureScreenshot = false;

      // Hide optional game object if set
      if (hideGameObject != null)
      {
        hideGameObject.SetActive(false);
      }

      // Create screenshot objects if needed
      if (renderTexture == null)
      {
        // Creates off-screen render texture that can rendered into
        rect = new Rect(0, 0, captureWidth, captureHeight);
        renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
      }

      // Get main camera and manually render scene to the render texture
      Camera camera = this.GetComponent<Camera>();
      camera.targetTexture = renderTexture;
      camera.Render();

      // Read pixels will read from the currently active render texture so make the offscreen render texture active and
      // then read the pixels
      RenderTexture.active = renderTexture;
      screenShot.ReadPixels(rect, 0, 0);

      // Reset active camera texture and render texture
      camera.targetTexture = null;
      RenderTexture.active = null;

      // Get our unique filename
      string filename = uniqueFilename((int) rect.width, (int) rect.height);

      // Pull in our file header/data bytes for the specified image format
      // Note: This has to be done from main thread
      byte[] fileHeader = null;
      byte[] fileData = null;
      if (format == Format.PNG)
      {
        fileData = screenShot.EncodeToPNG();
      }
      else if (format == Format.JPG)
      {
        fileData = screenShot.EncodeToJPG();
      }
      lastScreenshot = fileData;

      // Optionally save the captured screenshot
      if (saveCapture)
      {
        // Create new thread to save the image to file (only operation that can be done in background)
        new System.Threading.Thread(() =>
        {
          // Create file and write optional header with image bytes
          var f = System.IO.File.Create(filename);
          if (fileHeader != null) f.Write(fileHeader, 0, fileHeader.Length);
          f.Write(fileData, 0, fileData.Length);
          f.Close();
          Debug.Log(string.Format("Wrote screenshot {0} of size {1}", filename, fileData.Length));
        }).Start();
      }

      // Unhide optional game object if set
      if (hideGameObject != null) hideGameObject.SetActive(true);

      // Cleanup if needed
      if (optimizeForManyScreenshots == false)
      {
        Destroy(renderTexture);
        renderTexture = null;
        screenShot = null;
      }
    }
  }
}

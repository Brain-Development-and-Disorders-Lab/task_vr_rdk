/**
  File: CaptureManager.cs
  Author: Henry Burgess <henry.burgess@wustl.edu>

  Adapted from https://discussions.unity.com/t/how-to-save-a-picture-take-screenshot-from-a-camera-in-game/5792/8
*/
using UnityEngine;
using System.IO;

/// <summary>
/// `CaptureManager` will save individual images of active scene in any resolution and of a specific image
/// _format including jpg and png.
/// </summary>
public class CaptureManager : MonoBehaviour
{
    // Capture dimensions
    [SerializeField]
    private int _captureWidth = 1280;
    [SerializeField]
    private int _captureHeight = 720;
    [SerializeField]
    private bool _saveCapture = false;

    // Optional game object to hide during screenshots
    [SerializeField]
    private GameObject _hideGameObject;

    // Optimize for many screenshots will not destroy any objects so future screenshots will be fast
    [SerializeField]
    private bool _optimizeForManyScreenshots = true;

    public enum EFormat { JPG, PNG };
    [SerializeField]
    private EFormat _format = EFormat.JPG;

    // Folder to write output (defaults to data path)
    [SerializeField]
    private string _folder;

    // Private variables for screenshot generation
    private Rect _rect;
    private RenderTexture _renderTexture;
    private Texture2D _screenShot;
    private int _counter = 0;
    private byte[] _lastScreenshot;

    // Command flags
    private bool _captureScreenshot = false;

    /// <summary>
    /// Create a unique filename for images
    /// </summary>
    /// <param name="width">Image pixel width</param>
    /// <param name="height">Image pixel height</param>
    /// <returns></returns>
    private string UniqueFilename(int width, int height)
    {
        // if _folder not specified by now use a good default
        if (_folder == null || _folder.Length == 0)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                // On Android, use the persistent data path which is writable
                _folder = Application.persistentDataPath;
            }
            else
            {
                _folder = Application.dataPath;
                if (Application.isEditor)
                {
                    // put screenshots in _folder above asset path so unity doesn't index the files
                    string stringPath = _folder + "/..";
                    _folder = Path.GetFullPath(stringPath);
                }
            }
            _folder += "/screenshots";

            try
            {
                // make sure directory exists
                if (!Directory.Exists(_folder))
                {
                    Directory.CreateDirectory(_folder);
                }

                // count number of files of specified format in folder
                string mask = string.Format("screen_{0}x{1}*.{2}", width, height, _format.ToString().ToLower());
                _counter = Directory.GetFiles(_folder, mask, SearchOption.TopDirectoryOnly).Length;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating screenshot directory: {e.Message}");
                // Fallback to a temporary directory if we can't create the screenshots directory
                _folder = Application.temporaryCachePath;
                Debug.Log($"Falling back to temporary directory: {_folder}");
            }
        }

        // use width, height, and _counter for unique file name
        string filename = string.Format("{0}/screen_{1}x{2}_{3}.{4}", _folder, width, height, _counter, _format.ToString().ToLower());

        // up _counter for next call
        ++_counter;

        // return unique filename
        return filename;
    }

    /// <summary>
    /// Utility function to externally trigger screenshot capture
    /// </summary>
    public void CaptureScreenshot() => _captureScreenshot = true;

    /// <summary>
    /// Retrieve the most recent screenshot captured
    /// </summary>
    /// <returns>Array of bytes representing the screenshot data</returns>
    public byte[] GetLastScreenshot()
    {
        if (_lastScreenshot == null || _lastScreenshot.Length == 0)
        {
            return new byte[0];
        }
        else
        {
            return _lastScreenshot;
        }
    }

    private void Update()
    {
        if (_captureScreenshot)
        {
            _captureScreenshot = false;

            // Hide optional game object if set
            if (_hideGameObject != null)
            {
                _hideGameObject.SetActive(false);
            }

            // Create screenshot objects if needed
            if (_renderTexture == null)
            {
                // Creates off-screen render texture that can rendered into
                _rect = new Rect(0, 0, _captureWidth, _captureHeight);
                _renderTexture = new RenderTexture(_captureWidth, _captureHeight, 24);
                _screenShot = new Texture2D(_captureWidth, _captureHeight, TextureFormat.RGB24, false);
            }

            // Get main camera and manually render scene to the render texture
            var camera = GetComponent<Camera>();
            camera.targetTexture = _renderTexture;
            camera.Render();

            // Read pixels will read from the currently active render texture so make the offscreen render texture active and
            // then read the pixels
            RenderTexture.active = _renderTexture;
            _screenShot.ReadPixels(_rect, 0, 0);

            // Reset active camera texture and render texture
            camera.targetTexture = null;
            RenderTexture.active = null;

            // Get our unique filename
            string filename = UniqueFilename((int)_rect.width, (int)_rect.height);

            // Pull in our file header/data bytes for the specified image _format
            // Note: This has to be done from main thread
            byte[] fileHeader = null;
            byte[] fileData = null;
            if (_format == EFormat.PNG)
            {
                fileData = _screenShot.EncodeToPNG();
            }
            else if (_format == EFormat.JPG)
            {
                fileData = _screenShot.EncodeToJPG();
            }
            _lastScreenshot = fileData;

            // Optionally save the captured screenshot
            if (_saveCapture)
            {
                // Create new thread to save the image to file (only operation that can be done in background)
                new System.Threading.Thread(() =>
                {
                    // Create file and write optional header with image bytes
                    var f = File.Create(filename);
                    if (fileHeader != null)
                    {
                        f.Write(fileHeader, 0, fileHeader.Length);
                    }
                    f.Write(fileData, 0, fileData.Length);
                    f.Close();
                    Debug.Log(string.Format("Wrote screenshot {0} of size {1}", filename, fileData.Length));
                }).Start();
            }

            // Unhide optional game object if set
            if (_hideGameObject != null)
            {
                _hideGameObject.SetActive(true);
            }

            // Cleanup if needed
            if (!_optimizeForManyScreenshots)
            {
                Destroy(_renderTexture);
                _renderTexture = null;
                _screenShot = null;
            }
        }
    }
}

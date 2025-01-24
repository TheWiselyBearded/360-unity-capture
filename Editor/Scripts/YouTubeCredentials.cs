using UnityEngine;

[CreateAssetMenu(fileName = "YouTubeCredentials", menuName = "YouTube/Uploader Credentials")]
public class YouTubeCredentials : ScriptableObject {
    [Tooltip("Google API Client ID")]
    public string clientId;

    [Tooltip("Google API Client Secret")]
    public string clientSecret;

    [Tooltip("OAuth2 Redirect URI")]
    public string redirectUri = "http://localhost:8080/";
}

using System;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml;

namespace XmlNotepad {

    public class XmlProxyResolver : XmlUrlResolver {
        WebProxyService ps;

        public XmlProxyResolver(IServiceProvider site) {
            ps = site.GetService(typeof(WebProxyService)) as WebProxyService;
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {
            if (absoluteUri == null) {
                throw new ArgumentNullException("absoluteUri");
            }
            if (absoluteUri.Scheme == "http" && (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream))) {
                try {
                    return GetResponse(absoluteUri);
                } catch (Exception e) {
                    if (WebProxyService.ProxyAuthenticationRequired(e)) {
                        WebProxyState state = ps.PrepareWebProxy(this.Proxy, absoluteUri.AbsoluteUri, WebProxyState.DefaultCredentials, true);
                        if (state != WebProxyState.Abort) {
                            // try again...
                            return GetResponse(absoluteUri);
                        }
                    }
                    throw;
                }

            } else {
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }

        Stream GetResponse(Uri uri) {
            WebRequest webReq = WebRequest.Create(uri);
            webReq.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            webReq.Credentials = CredentialCache.DefaultCredentials;
            webReq.Proxy = this.Proxy;
            WebResponse resp = webReq.GetResponse();
            return resp.GetResponseStream();
        }

        IWebProxy Proxy {
            get { return HttpWebRequest.DefaultWebProxy; }
        }
    }

    enum WebProxyState {
        NoCredentials = 0,
        DefaultCredentials = 1,
        CachedCredentials = 2,
        PromptForCredentials = 3,
        Abort = 4
    } ;


    internal class WebProxyService {
        private IServiceProvider site;
        private NetworkCredential cachedCredentials;
        private string currentProxyUrl;

        public WebProxyService(IServiceProvider site) {
            this.site = site;
        }

        //---------------------------------------------------------------------
        // public methods
        //---------------------------------------------------------------------
        public static bool ProxyAuthenticationRequired(Exception ex) {
            bool authNeeded = false;

            System.Net.WebException wex = ex as System.Net.WebException;

            if ((wex != null) && (wex.Status == System.Net.WebExceptionStatus.ProtocolError)) {
                System.Net.HttpWebResponse hwr = wex.Response as System.Net.HttpWebResponse;
                if ((hwr != null) && (hwr.StatusCode == System.Net.HttpStatusCode.ProxyAuthenticationRequired)) {
                    authNeeded = true;
                }
            }

            return authNeeded;
        }

        /// <summary>
        /// This method attaches credentials to the web proxy object.
        /// </summary>
        /// <param name="proxy">The proxy to attach credentials to.</param>
        /// <param name="webCallUrl">The url for the web call.</param>
        /// <param name="oldProxyState">The current state fo the web call.</param>
        /// <param name="newProxyState">The new state for the web call.</param>
        /// <param name="okToPrompt">Prompt user for credentials if they are not available.</param>
        public WebProxyState PrepareWebProxy(IWebProxy proxy, string webCallUrl, WebProxyState oldProxyState, bool okToPrompt) {
            WebProxyState newProxyState = WebProxyState.Abort;

            if (string.IsNullOrEmpty(webCallUrl)) {
                Debug.Fail("PrepareWebProxy called with an empty WebCallUrl.");
                webCallUrl = "http://go.microsoft.com/fwlink/?LinkId=81947";
            }

            // Get the web proxy url for the the current web call.
            Uri webCallProxy = null;
            if (proxy != null) {
                webCallProxy = proxy.GetProxy(new Uri(webCallUrl));
            }

            if ((proxy != null) && (webCallProxy != null)) {
                // get proxy url.
                string proxyUrl = webCallProxy.Host;
                if (string.IsNullOrEmpty(currentProxyUrl)) {
                    currentProxyUrl = proxyUrl;
                }

                switch (oldProxyState) {
                    case WebProxyState.NoCredentials:
                        // Add the default credentials only if there aren't any credentials attached to
                        // the DefaultWebProxy. If the first calls attaches the correct credentials, the
                        // second call will just use them, instead of overwriting it with the default credentials.
                        // This avoids multiple web calls. Note that state is transitioned to DefaultCredentials
                        // instead of CachedCredentials. This ensures that web calls be tried with the
                        // cached credentials if the currently attached credentials don't result in successful web call.
                        if ((proxy.Credentials == null)) {
                            proxy.Credentials = CredentialCache.DefaultCredentials;
                        }
                        newProxyState = WebProxyState.DefaultCredentials;
                        break;

                    case WebProxyState.DefaultCredentials:
                        // Fetch cached credentials if they are null or if the proxy url has changed.
                        if ((cachedCredentials == null) ||
                            !string.Equals(currentProxyUrl, proxyUrl, StringComparison.OrdinalIgnoreCase)) {
                            cachedCredentials = GetCachedCredentials(proxyUrl);
                        }

                        if (cachedCredentials != null) {
                            proxy.Credentials = cachedCredentials;
                            newProxyState = WebProxyState.CachedCredentials;
                            break;
                        }

                        // Proceed to next step if cached credentials are not available.
                        goto case WebProxyState.CachedCredentials;

                    case WebProxyState.CachedCredentials:
                    case WebProxyState.PromptForCredentials:
                        if (okToPrompt) {
                            if (DialogResult.OK == PromptForCredentials(proxyUrl)) {
                                proxy.Credentials = cachedCredentials;
                                newProxyState = WebProxyState.PromptForCredentials;
                            } else {
                                newProxyState = WebProxyState.Abort;
                            }
                        } else {
                            newProxyState = WebProxyState.Abort;
                        }
                        break;

                    case WebProxyState.Abort:
                        throw new InvalidOperationException();

                    default:
                        throw new ArgumentException(string.Empty, "oldProxyState");
                }
            } else {
                // No proxy for the webCallUrl scenario.
                if (oldProxyState == WebProxyState.NoCredentials) {
                    // if it is the first call, change the state and let the web call proceed.
                    newProxyState = WebProxyState.DefaultCredentials;
                } else {
                    Debug.Fail("This method is called a second time when 407 occurs. A 407 shouldn't have occurred as there is no default proxy.");
                    // We dont have a good idea of the circumstances under which
                    // WebProxy might be null for a url. To be safe, for VS 2005 SP1,
                    // we will just return the abort state, instead of throwing
                    // an exception. Abort state will ensure that no further procesing
                    // occurs and we will not bring down the app.
                    // throw new InvalidOperationException();
                    newProxyState = WebProxyState.Abort;
                }
            }
            return newProxyState;
        }

        //---------------------------------------------------------------------
        // private methods
        //---------------------------------------------------------------------
        /// <summary>
        /// Retrieves credentials from the credential store.
        /// </summary>
        /// <param name="proxyUrl">The proxy url for which credentials are retrieved.</param>
        /// <returns>The credentails for the proxy.</returns>
        private NetworkCredential GetCachedCredentials(string proxyUrl) {
            return Credentials.GetCachedCredentials(proxyUrl);
        }

        /// <summary>
        /// Prompt the use to provider credentials and optionally store them.
        /// </summary>
        /// <param name="proxyUrl">The server that requires credentials.</param>
        /// <returns>Returns the dialog result of the prompt dialog.</returns>
        private DialogResult PromptForCredentials(string proxyUrl) {
            DialogResult dialogResult = DialogResult.Cancel;
            bool prompt = true;
            while (prompt) {
                prompt = false;

                NetworkCredential cred;
                dialogResult = Credentials.PromptForCredentials(proxyUrl, out cred);
                if (DialogResult.OK == dialogResult) {
                    if (cred != null) {
                        cachedCredentials = cred;
                        currentProxyUrl = proxyUrl;
                    } else {
                        // Prompt again for credential as we are not able to create
                        // a NetworkCredential object from the supplied credentials.
                        prompt = true;
                    }
                }
            }

            return dialogResult;
        }

    }

    internal sealed class Credentials {
        /// <summary>
        /// Prompt the user for credentials.
        /// </summary>
        /// <param name="target">
        /// The credential target. It is displayed in the prompt dialog and is
        /// used for credential storage.
        /// </param>
        /// <param name="credential">The user supplied credentials.</param>
        /// <returns>
        /// DialogResult.OK = if Successfully prompted user for credentials.
        /// DialogResult.Cancel = if user cancelled the prompt dialog.
        /// </returns>
        public static DialogResult PromptForCredentials(string target, out NetworkCredential credential) {
            DialogResult dr = DialogResult.Cancel;
            credential = null;
            string username;
            string password;

            IntPtr hwndOwner = IntPtr.Zero;
            // Show the OS credential dialog.
            dr = ShowOSCredentialDialog(target, hwndOwner, out username, out password);
            // Create the NetworkCredential object.
            if (dr == DialogResult.OK) {
                credential = CreateCredentials(username, password, target);
            }

            return dr;
        }

        /// <summary>
        /// Get the cached credentials from the credentials store.
        /// </summary>
        /// <param name="target">The credential target.</param>
        /// <returns>
        /// The cached credentials. It will return null if credentails are found
        /// in the cache.
        /// </returns>
        public static NetworkCredential GetCachedCredentials(string target) {
            NetworkCredential cred = null;

            string username;
            string password;

            // Retrieve credentials from the OS credential store.
            if (ReadOSCredentials(target, out username, out password)) {
                // Create the NetworkCredential object if we successfully
                // retrieved the credentails from the OS store.
                cred = CreateCredentials(username, password, target);
            }

            return cred;

        }

        //---------------------------------------------------------------------
        // private methods
        //---------------------------------------------------------------------


        /// <summary>
        /// This function calls the OS dialog to prompt user for credential.
        /// </summary>
        /// <param name="target">
        /// The credential target. It is displayed in the prompt dialog and is
        /// used for credential storage.
        /// </param>
        /// <param name="hwdOwner">The parent for the dialog.</param>
        /// <param name="userName">The username supplied by the user.</param>
        /// <param name="password">The password supplied by the user.</param>
        /// <returns>
        /// DialogResult.OK = if Successfully prompted user for credentials.
        /// DialogResult.Cancel = if user cancelled the prompt dialog.
        /// </returns>
        private static DialogResult ShowOSCredentialDialog(string target, IntPtr hwdOwner, out string userName, out string password) {
            DialogResult retValue = DialogResult.Cancel;
            userName = string.Empty;
            password = string.Empty;

            string titleFormat = SR.CredentialDialog_TitleFormat;
            string descriptionFormat = SR.CredentialDialog_DescriptionTextFormat;

            // Create the CREDUI_INFO structure. 
            NativeMethods.CREDUI_INFO info = new NativeMethods.CREDUI_INFO();
            info.pszCaptionText = string.Format(CultureInfo.CurrentUICulture, titleFormat, target);
            info.pszMessageText = string.Format(CultureInfo.CurrentUICulture, descriptionFormat, target);
            info.hwndParentCERParent = hwdOwner;
            info.hbmBannerCERHandle = IntPtr.Zero;
            info.cbSize = Marshal.SizeOf(info);

            // We do not use CREDUI_FLAGS_VALIDATE_USERNAME flag as it doesn't allow plain user
            // (one with no domain component). Instead we use CREDUI_FLAGS_COMPLETE_USERNAME.
            // It does some basic username validation (like doesnt allow two "\" in the user name.
            // It does adds the target to the username. For example, if user entered "foo" for
            // taget "bar.com", it will return username as "bar.com\foo". We trim out bar.com
            // while parsing the username. User can input "foo@bar.com" as workaround to provide
            // "bar.com\foo" as the username.
            // We specify CRED_TYPE_SERVER_CREDENTIAL flag as the stored credentials appear in the 
            // "Control Panel->Stored Usernames and Password". It is how IE stores and retrieve
            // credentials. By using the CRED_TYPE_SERVER_CREDENTIAL flag allows IE and VS to
            // share credentials.
            // We dont specify the CREDUI_FLAGS_EXPECT_CONFIRMATION as the VS proxy service consumers
            // dont call back into the service to confirm that the call succeeded.
            NativeMethods.CREDUI_FLAGS flags = NativeMethods.CREDUI_FLAGS.SERVER_CREDENTIAL |
                                                NativeMethods.CREDUI_FLAGS.SHOW_SAVE_CHECK_BOX |
                                                NativeMethods.CREDUI_FLAGS.COMPLETE_USERNAME |
                                                NativeMethods.CREDUI_FLAGS.EXCLUDE_CERTIFICATES;

            StringBuilder user = new StringBuilder(Convert.ToInt32(NativeMethods.CREDUI_MAX_USERNAME_LENGTH));
            StringBuilder pwd = new StringBuilder(Convert.ToInt32(NativeMethods.CREDUI_MAX_PASSWORD_LENGTH));
            int saveCredentials = 0;
            // Ensures that CredUPPromptForCredentials results in a prompt.
            int netError = NativeMethods.ERROR_LOGON_FAILURE;

            // Call the OS API to prompt for credentials.
            NativeMethods.CredUIReturnCodes result = NativeMethods.CredUIPromptForCredentials(
                info,
                target,
                IntPtr.Zero,
                netError,
                user,
                NativeMethods.CREDUI_MAX_USERNAME_LENGTH,
                pwd,
                NativeMethods.CREDUI_MAX_PASSWORD_LENGTH,
                ref saveCredentials,
                flags);


            if (result == NativeMethods.CredUIReturnCodes.NO_ERROR) {
                userName = user.ToString();
                password = pwd.ToString();

                try {
                    if (Convert.ToBoolean(saveCredentials)) {
                        // Try reading the credentials back to ensure that we can read the stored credentials. If
                        // the CredUIPromptForCredentials() function is not able successfully call CredGetTargetInfo(),
                        // it will store credentials with credential type as DOMAIN_PASSWORD. For DOMAIN_PASSWORD
                        // credential type we can only retrive the user name. As a workaround, we store the credentials
                        // as credential type as GENERIC.
                        string storedUserName;
                        string storedPassword;
                        bool successfullyReadCredentials = ReadOSCredentials(target, out storedUserName, out storedPassword);
                        if (!successfullyReadCredentials ||
                            !string.Equals(userName, storedUserName, StringComparison.Ordinal) ||
                            !string.Equals(password, storedPassword, StringComparison.Ordinal)) {
                            // We are not able to retrieve the credentials. Try storing them as GENERIC credetails.

                            // Create the NativeCredential object.
                            NativeMethods.NativeCredential customCredential = new NativeMethods.NativeCredential();
                            customCredential.userName = userName;
                            customCredential.type = NativeMethods.CRED_TYPE_GENERIC;
                            customCredential.targetName = CreateCustomTarget(target);
                            // Store credentials across sessions.
                            customCredential.persist = (uint)NativeMethods.CRED_PERSIST.LOCAL_MACHINE;
                            if (!string.IsNullOrEmpty(password)) {
                                customCredential.credentialBlobSize = (uint)Marshal.SystemDefaultCharSize * ((uint)password.Length);
                                customCredential.credentialBlob = Marshal.StringToCoTaskMemAuto(password);
                            }

                            try {
                                NativeMethods.CredWrite(ref customCredential, 0);
                            } finally {
                                if (customCredential.credentialBlob != IntPtr.Zero) {
                                    Marshal.FreeCoTaskMem(customCredential.credentialBlob);
                                }

                            }
                        }
                    }
                } catch {
                    // Ignore that failure to read back the credentials. We still have
                    // username and password to use in the current session.
                }

                retValue = DialogResult.OK;
            } else if (result == NativeMethods.CredUIReturnCodes.ERROR_CANCELLED) {
                retValue = DialogResult.Cancel;
            } else {
                Debug.Fail("CredUIPromptForCredentials failed with result = " + result.ToString());
                retValue = DialogResult.Cancel;
            }

            info.Dispose();
            return retValue;
        }

        /// <summary>
        /// Generates a NetworkCredential object from username and password. The function will
        /// parse username part and invoke the correct NetworkCredential construction.
        /// </summary>
        /// <param name="username">username retrieved from user/registry.</param>
        /// <param name="password">password retrieved from user/registry.</param>
        /// <returns></returns>
        private static NetworkCredential CreateCredentials(string username, string password, string targetServer) {
            NetworkCredential cred = null;

            if ((!string.IsNullOrEmpty(username)) && (!string.IsNullOrEmpty(password))) {
                string domain;
                string user;
                if (ParseUsername(username, targetServer, out user, out domain)) {
                    if (string.IsNullOrEmpty(domain)) {
                        cred = new NetworkCredential(user, password);
                    } else {
                        cred = new NetworkCredential(user, password, domain);
                    }
                }
            }

            return cred;
        }

        /// <summary>
        /// This fuction calls CredUIParseUserName() to parse the user name.
        /// </summary>
        /// <param name="username">The username name to pass.</param>
        /// <param name="targetServer">The target for which username is being parsed.</param>
        /// <param name="user">The user part of the username.</param>
        /// <param name="domain">The domain part of the username.</param>
        /// <returns>Returns true if it successfully parsed the username.</returns>
        private static bool ParseUsername(string username, string targetServer, out string user, out string domain) {
            user = string.Empty;
            domain = string.Empty;

            if (string.IsNullOrEmpty(username)) {
                return false;
            }

            bool successfullyParsed = true;

            StringBuilder strUser = new StringBuilder(Convert.ToInt32(NativeMethods.CREDUI_MAX_USERNAME_LENGTH));
            StringBuilder strDomain = new StringBuilder(Convert.ToInt32(NativeMethods.CREDUI_MAX_DOMAIN_TARGET_LENGTH));
            // Call the OS API to do the parsing.
            NativeMethods.CredUIReturnCodes result = NativeMethods.CredUIParseUserName(username,
                                                    strUser,
                                                    NativeMethods.CREDUI_MAX_USERNAME_LENGTH,
                                                    strDomain,
                                                    NativeMethods.CREDUI_MAX_DOMAIN_TARGET_LENGTH);

            successfullyParsed = (result == NativeMethods.CredUIReturnCodes.NO_ERROR);

            if (successfullyParsed) {
                user = strUser.ToString();
                domain = strDomain.ToString();

                // Remove the domain part if domain is same as target. This is to workaround
                // the COMPLETE_USERNAME flag which add the target to the user name as the 
                // domain component.
                // Read comments in ShowOSCredentialDialog() for more details.
                if (!string.IsNullOrEmpty(domain) &&
                    string.Equals(domain, targetServer, StringComparison.OrdinalIgnoreCase)) {
                    domain = string.Empty;
                }
            }

            return successfullyParsed;
        }

        /// <summary>
        /// Retrieves credentials from the OS store.
        /// </summary>
        /// <param name="target">The credential target.</param>
        /// <param name="username">The retrieved username.</param>
        /// <param name="password">The retrieved password.</param>
        /// <returns>Returns true if it successfully reads the OS credentials.</returns>
        private static bool ReadOSCredentials(string target, out string username, out string password) {
            username = string.Empty;
            password = string.Empty;

            if (string.IsNullOrEmpty(target)) {
                return false;
            }

            IntPtr credPtr = IntPtr.Zero;
            IntPtr customCredPtr = IntPtr.Zero;

            try {
                bool queriedDomainPassword = false;
                bool readCredentials = true;

                // Query the OS credential store.
                if (!NativeMethods.CredRead(
                        target,
                        NativeMethods.CRED_TYPE_GENERIC,
                        0,
                        out credPtr)) {
                    readCredentials = false;

                    // Query for the DOMAIN_PASSWORD credential type to retrieve the 
                    // credentials. CredUPromptForCredentials will store credentials
                    // as DOMAIN_PASSWORD credential type if it is not able to resolve
                    // the target using CredGetTargetInfo() function.
                    if (Marshal.GetLastWin32Error() == NativeMethods.ERROR_NOT_FOUND) {
                        queriedDomainPassword = true;
                        // try queryiing for CRED_TYPE_DOMAIN_PASSWORD
                        if (NativeMethods.CredRead(
                            target,
                            NativeMethods.CRED_TYPE_DOMAIN_PASSWORD,
                            0,
                            out credPtr)) {
                            readCredentials = true;
                        }
                    }
                }

                if (readCredentials) {
                    // Get the native credentials if CredRead succeeds.
                    NativeMethods.NativeCredential nativeCredential = (NativeMethods.NativeCredential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.NativeCredential));
                    password = (nativeCredential.credentialBlob != IntPtr.Zero) ?
                                            Marshal.PtrToStringUni(nativeCredential.credentialBlob, (int)(nativeCredential.credentialBlobSize / Marshal.SystemDefaultCharSize))
                                            : string.Empty;

                    username = nativeCredential.userName;

                    // If we retrieved the username using the credentials type as DOMAIN_PASSWORD, and 
                    // we are not able to retrieve password, we query the GENERIC credentials to
                    // retrieve the password. Read comments in the ShowOSCredentialDialog() funtion
                    // for more details.
                    if (string.IsNullOrEmpty(password) && queriedDomainPassword) {
                        // Read custom credentials.
                        if (NativeMethods.CredRead(
                                CreateCustomTarget(target),
                                NativeMethods.CRED_TYPE_GENERIC,
                                0,
                                out customCredPtr)) {
                            NativeMethods.NativeCredential customNativeCredential = (NativeMethods.NativeCredential)Marshal.PtrToStructure(customCredPtr, typeof(NativeMethods.NativeCredential));
                            if (string.Equals(username, customNativeCredential.userName, StringComparison.OrdinalIgnoreCase)) {
                                password = (customNativeCredential.credentialBlob != IntPtr.Zero) ?
                                                        Marshal.PtrToStringUni(customNativeCredential.credentialBlob, (int)(customNativeCredential.credentialBlobSize / Marshal.SystemDefaultCharSize))
                                                        : string.Empty;
                            }
                        }
                    }
                }
            } catch (Exception) {
                username = string.Empty;
                password = string.Empty;
            } finally {
                if (credPtr != IntPtr.Zero) {
                    NativeMethods.CredFree(credPtr);
                }

                if (customCredPtr != IntPtr.Zero) {
                    NativeMethods.CredFree(customCredPtr);
                }
            }

            bool successfullyReadCredentials = true;

            if (string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(password)) {
                username = string.Empty;
                password = string.Empty;
                successfullyReadCredentials = false;
            }

            return successfullyReadCredentials;
        }

        /// <summary>
        /// Generates the generic target name.
        /// </summary>
        /// <param name="target">The credetial target.</param>
        /// <returns>The generic target.</returns>
        private static string CreateCustomTarget(string target) {
            if (string.IsNullOrEmpty(target)) {
                return string.Empty;
            }

            return "Credentials_" + target;
        }

    }

    #region NativeMethods

    static class NativeMethods {
        private const string advapi32Dll = "advapi32.dll";
        private const string credUIDll = "credui.dll";
        private const string user32Dll = "User32.dll";
        private const string sensapiDll = "sensapi.dll";

        public const int
        ERROR_INVALID_FLAGS = 1004,  // Invalid flags.
        ERROR_NOT_FOUND = 1168,  // Element not found.
        ERROR_NO_SUCH_LOGON_SESSION = 1312,  // A specified logon session does not exist. It may already have been terminated.
        ERROR_LOGON_FAILURE = 1326;  // Logon failure: unknown user name or bad password.

        [Flags]
        public enum CREDUI_FLAGS : uint {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            REQUEST_ADMINISTRATOR = 0x4,
            EXCLUDE_CERTIFICATES = 0x8,
            REQUIRE_CERTIFICATE = 0x10,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            REQUIRE_SMARTCARD = 0x100,
            PASSWORD_ONLY_OK = 0x200,
            VALIDATE_USERNAME = 0x400,
            COMPLETE_USERNAME = 0x800,
            PERSIST = 0x1000,
            SERVER_CREDENTIAL = 0x4000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            KEEP_USERNAME = 0x100000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public class CREDUI_INFO : IDisposable {
            public int cbSize;
            public IntPtr hwndParentCERParent;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMessageText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCaptionText;
            public IntPtr hbmBannerCERHandle;

            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing) {

                if (disposing) {
                    // Release managed resources.
                }
                // Free the unmanaged resource ...
                hwndParentCERParent = IntPtr.Zero;
                hbmBannerCERHandle = IntPtr.Zero;

            }

            ~CREDUI_INFO() {
                Dispose(false);
            }
        }

        public enum CredUIReturnCodes : uint {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004,
        }

        // Copied from wincred.h
        public const uint
            // Values of the Credential Type field.
        CRED_TYPE_GENERIC = 1,
        CRED_TYPE_DOMAIN_PASSWORD = 2,
        CRED_TYPE_DOMAIN_CERTIFICATE = 3,
        CRED_TYPE_DOMAIN_VISIBLE_PASSWORD = 4,
        CRED_TYPE_MAXIMUM = 5,                           // Maximum supported cred type
        CRED_TYPE_MAXIMUM_EX = (CRED_TYPE_MAXIMUM + 1000),    // Allow new applications to run on old OSes

        // String limits
        CRED_MAX_CREDENTIAL_BLOB_SIZE = 512,         // Maximum size of the CredBlob field (in bytes)
        CRED_MAX_STRING_LENGTH = 256,         // Maximum length of the various credential string fields (in characters)
        CRED_MAX_USERNAME_LENGTH = (256 + 1 + 256), // Maximum length of the UserName field.  The worst case is <User>@<DnsDomain>
        CRED_MAX_GENERIC_TARGET_NAME_LENGTH = 32767,       // Maximum length of the TargetName field for CRED_TYPE_GENERIC (in characters)
        CRED_MAX_DOMAIN_TARGET_NAME_LENGTH = (256 + 1 + 80),  // Maximum length of the TargetName field for CRED_TYPE_DOMAIN_* (in characters). Largest one is <DfsRoot>\<DfsShare>
        CRED_MAX_VALUE_SIZE = 256,         // Maximum size of the Credential Attribute Value field (in bytes)
        CRED_MAX_ATTRIBUTES = 64,          // Maximum number of attributes per credential
        CREDUI_MAX_MESSAGE_LENGTH = 32767,
        CREDUI_MAX_CAPTION_LENGTH = 128,
        CREDUI_MAX_GENERIC_TARGET_LENGTH = CRED_MAX_GENERIC_TARGET_NAME_LENGTH,
        CREDUI_MAX_DOMAIN_TARGET_LENGTH = CRED_MAX_DOMAIN_TARGET_NAME_LENGTH,
        CREDUI_MAX_USERNAME_LENGTH = CRED_MAX_USERNAME_LENGTH,
        CREDUI_MAX_PASSWORD_LENGTH = (CRED_MAX_CREDENTIAL_BLOB_SIZE / 2);

        internal enum CRED_PERSIST : uint {
            NONE = 0,
            SESSION = 1,
            LOCAL_MACHINE = 2,
            ENTERPRISE = 3,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NativeCredential {
            public uint flags;
            public uint type;
            public string targetName;
            public string comment;
            public int lastWritten_lowDateTime;
            public int lastWritten_highDateTime;
            public uint credentialBlobSize;
            public IntPtr credentialBlob;
            public uint persist;
            public uint attributeCount;
            public IntPtr attributes;
            public string targetAlias;
            public string userName;
        };

        [DllImport(advapi32Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredReadW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool
        CredRead(
            [MarshalAs(UnmanagedType.LPWStr)]
			string targetName,
            [MarshalAs(UnmanagedType.U4)]
			uint type,
            [MarshalAs(UnmanagedType.U4)]
			uint flags,
            out IntPtr credential
            );

        [DllImport(advapi32Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredWriteW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool
        CredWrite(
            ref NativeCredential Credential,
            [MarshalAs(UnmanagedType.U4)]
			uint flags
            );

        [DllImport(advapi32Dll)]
        public static extern void
        CredFree(
            IntPtr buffer
            );

        [DllImport(credUIDll, EntryPoint = "CredUIPromptForCredentialsW", CharSet = CharSet.Unicode)]
        public static extern CredUIReturnCodes CredUIPromptForCredentials(
            CREDUI_INFO pUiInfo,  // Optional (one can pass null here)
            [MarshalAs(UnmanagedType.LPWStr)]
			string targetName,
            IntPtr Reserved,      // Must be 0 (IntPtr.Zero)
            int iError,
            [MarshalAs(UnmanagedType.LPWStr)]
			StringBuilder pszUserName,
            [MarshalAs(UnmanagedType.U4)]
			uint ulUserNameMaxChars,
            [MarshalAs(UnmanagedType.LPWStr)]
			StringBuilder pszPassword,
            [MarshalAs(UnmanagedType.U4)]
			uint ulPasswordMaxChars,
            ref int pfSave,
            CREDUI_FLAGS dwFlags);

        /// <returns>
        /// Win32 system errors:
        /// NO_ERROR
        /// ERROR_INVALID_ACCOUNT_NAME
        /// ERROR_INSUFFICIENT_BUFFER
        /// ERROR_INVALID_PARAMETER
        /// </returns>
        [DllImport(credUIDll, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "CredUIParseUserNameW")]
        public static extern CredUIReturnCodes CredUIParseUserName(
            [MarshalAs(UnmanagedType.LPWStr)]
			string strUserName,
            [MarshalAs(UnmanagedType.LPWStr)]
			StringBuilder strUser,
            [MarshalAs(UnmanagedType.U4)]
			uint iUserMaxChars,
            [MarshalAs(UnmanagedType.LPWStr)]
			StringBuilder strDomain,
            [MarshalAs(UnmanagedType.U4)]
			uint iDomainMaxChars
            );
    }

    #endregion
}

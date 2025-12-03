# 🚀 START HERE - SampleSPA Setup

## You're Getting an Error? Fix It Now! ⚡

If you're seeing this error when trying to login:
```
System.Text.Json.JsonReaderException: 'h' is an invalid start of a value
```

**Follow these 3 simple steps:**

### Step 1: Run the Database Fix Script 🔧

Open **SQL Server Management Studio** and run this script:

```
📄 setup-client.sql
```

This will:
- ✅ Fix malformed redirect URIs in your database
- ✅ Create/update the `web-app` client for SampleSPA
- ✅ Configure correct redirect URIs (`http://localhost:8080/callback.html`)

### Step 2: Restart Your Identity Server 🔄

Stop and restart your Identity Server to clear OpenIddict's cache.

### Step 3: Try Logging In Again ✨

1. Start the SampleSPA server:
   ```bash
   start.bat        # Windows
   ./start.sh       # Mac/Linux
   ```

2. Open your browser:
   ```
   http://localhost:8080/index.html
   ```

3. Click "Login with OAuth 2.0"

---

## What's Included in This Sample Project?

### 📁 Project Structure

```
SampleSPA/
├── 🌐 HTML Pages
│   ├── index.html           Main application page
│   └── callback.html        OAuth callback handler
│
├── 🎨 Styling
│   └── css/styles.css       Modern, responsive CSS
│
├── 💻 JavaScript
│   ├── js/config.js         Configuration (update this!)
│   ├── js/pkce-helper.js    PKCE implementation
│   ├── js/app.js            Main app logic
│   └── js/callback.js       Callback handler
│
├── 🛠️ Server Scripts
│   ├── serve.py             Python server
│   ├── serve.js             Node.js server
│   ├── start.bat            Windows startup
│   └── start.sh             Unix/Mac startup
│
├── 🗄️ Database Scripts
│   ├── setup-client.sql     Fix database & setup client
│   └── diagnose.sql         Diagnose issues
│
└── 📚 Documentation
    ├── README.md            Full documentation
    ├── QUICKSTART.md        Quick setup guide
    ├── fix-database-issue.md Detailed fix guide
    └── START_HERE.md        This file!
```

### ✨ Features

- ✅ OAuth 2.0 Authorization Code flow with PKCE
- ✅ Modern, responsive UI
- ✅ No external dependencies (vanilla JS)
- ✅ Secure token handling
- ✅ User profile display
- ✅ Token inspection and claims visualization
- ✅ Ready to use with Simple Identity Server

---

## Quick Start (After Fixing the Database)

### 1. Configure the Application

Edit `js/config.js` and verify these settings:

```javascript
export const config = {
    clientId: 'web-app',
    authorizationEndpoint: 'https://localhost:8443/connect/authorize',
    tokenEndpoint: 'https://localhost:8443/connect/token',
    // ... rest stays the same
};
```

> ⚠️ **Important**: Make sure the URLs match your Identity Server!

### 2. Start the Server

Choose your preferred method:

**Windows:**
```cmd
start.bat
```

**Mac/Linux:**
```bash
chmod +x start.sh
./start.sh
```

**Or manually:**
```bash
# Python
python serve.py

# Node.js
node serve.js
```

### 3. Open in Browser

Navigate to:
```
http://localhost:8080/index.html
```

### 4. Test the Flow

1. Click **"Login with OAuth 2.0"**
2. You'll be redirected to Identity Server
3. Enter your credentials (or register)
4. After login, you'll see your profile and tokens!

---

## Verification Checklist ✅

Before attempting login, verify:

- [ ] Identity Server is running on `https://localhost:8443` (or your configured URL)
- [ ] Database fix script (`setup-client.sql`) has been executed
- [ ] Identity Server has been restarted
- [ ] SampleSPA server is running on `http://localhost:8080`
- [ ] Configuration in `js/config.js` matches your Identity Server
- [ ] Browser can access both the Identity Server and SampleSPA

---

## Common Issues & Solutions

### Issue 1: Port Already in Use

If port 8080 is taken, change it in:
1. Server script (`serve.py` or `serve.js`)
2. Configuration (`js/config.js`)
3. Database client (`setup-client.sql`)

### Issue 2: CORS Errors

Make sure your Identity Server allows CORS from `http://localhost:8080`.

In Identity Server's `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### Issue 3: Certificate Errors (HTTPS)

For local development with self-signed certificates:
1. Navigate to your Identity Server URL in browser
2. Accept the certificate warning
3. Then try the SampleSPA again

### Issue 4: Still Getting JSON Error

1. Run the diagnostic script:
   ```sql
   diagnose.sql
   ```

2. Check the output for specific issues

3. Re-run the setup script:
   ```sql
   setup-client.sql
   ```

4. Verify in database:
   ```sql
   SELECT ClientId, RedirectUris, PostLogoutRedirectUris
   FROM OpenIddictApplications
   WHERE ClientId = 'web-app'
   ```

   The URIs should look like:
   ```json
   ["http://localhost:8080/callback.html", "https://localhost:8080/callback.html"]
   ```

---

## Testing Your Setup

### Manual Test

1. **Start Identity Server**
   ```bash
   cd SimpleIdentityServer.API
   dotnet run
   ```

2. **Start SampleSPA**
   ```bash
   cd SampleSPA
   start.bat  # or ./start.sh
   ```

3. **Open Browser**
   - Navigate to: `http://localhost:8080`
   - Open Developer Console (F12)
   - Click "Login with OAuth 2.0"
   - Check console for any errors

4. **Expected Flow**
   - Redirects to Identity Server login
   - Enter credentials
   - Redirects back to `callback.html`
   - Then redirects to `index.html` with your profile

### Debug Mode

To see detailed logs, open browser Developer Console (F12) and check:
- **Console tab**: JavaScript logs and errors
- **Network tab**: HTTP requests and responses
- **Application tab**: Session storage (check for `oauth_tokens`)

---

## Next Steps

Once everything is working:

1. **Explore the Code**
   - Check out `js/pkce-helper.js` to understand PKCE
   - Read `js/app.js` to see the authentication flow
   - Look at `css/styles.css` for styling

2. **Customize the UI**
   - Modify colors and branding in `css/styles.css`
   - Update text in `index.html`
   - Add your logo

3. **Add API Calls**
   - Use the access token to call protected APIs
   - Example in `README.md` under "Calling Protected APIs"

4. **Implement Additional Features**
   - Add logout functionality
   - Implement token refresh
   - Add user profile editing

---

## Getting Help

### Documentation Files

- **README.md** - Comprehensive documentation
- **QUICKSTART.md** - Fast setup guide
- **fix-database-issue.md** - Detailed database fix guide

### Check Logs

**Identity Server logs** (in console where you ran `dotnet run`):
- Look for OpenIddict errors
- Check for CORS issues
- Verify client registration

**Browser console** (F12):
- JavaScript errors
- Network failures
- PKCE issues

### Diagnostic Tools

Run these SQL scripts to diagnose issues:
```sql
-- Check client configuration
diagnose.sql

-- Fix and setup client
setup-client.sql
```

---

## Quick Reference

### Default Configuration

| Setting | Value |
|---------|-------|
| Identity Server | `https://localhost:8443` |
| SampleSPA | `http://localhost:8080` |
| Client ID | `web-app` |
| Redirect URI | `http://localhost:8080/callback.html` |
| Scopes | `openid profile email roles` |

### Important Files to Configure

1. **js/config.js** - Application configuration
2. **Database** - Client registration via SQL script

### Commands

```bash
# Start SampleSPA (Windows)
start.bat

# Start SampleSPA (Mac/Linux)
./start.sh

# Start with Python
python serve.py

# Start with Node.js
node serve.js
```

---

## Ready? Let's Go! 🎉

1. ✅ Run `setup-client.sql`
2. ✅ Restart Identity Server
3. ✅ Run `start.bat` or `./start.sh`
4. ✅ Open `http://localhost:8080`
5. ✅ Click "Login with OAuth 2.0"

**Happy coding!** 🚀


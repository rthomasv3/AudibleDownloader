# Audible Downloader

![Audible Downloader Screenshot](images/audible_downloader_screenshot.png)

A desktop application for downloading and managing your Audible audiobook library. This application provides direct access to your purchased audiobooks in DRM-free M4A format with full metadata and chapter preservation.

## Features

### Library Management
- View your complete Audible library with cover art, metadata, and book details
- Sort by title, author, release date, runtime, or series
- Filter by download status and availability

### Book Downloads
- Download audiobooks directly from Audible's CDN
- Automatic codec selection for optimal quality
- Support for multi-part audiobooks
- Real-time download progress tracking
- Automatic DRM removal and conversion to `.m4a` files

### Audio Processing
- Merge multi-part audiobooks into single files
- Optional trimming of Audible intro/outro messages between parts
- Complete metadata preservation including chapters, tags, and cover art
- Built-in FFmpeg integration for audio processing
- Progress tracking during merge operations

### Account Management
- Secure OAuth2 authentication with Audible
- Encrypted credential storage using system credential manager
- Automatic token refresh
- Device registration and deregistration

## System Requirements

- Windows, Linux, or macOS
- Active Audible account with purchased audiobooks

## Installation

1. Download the latest release for your platform
2. Extract the archive
3. Run the executable

## Usage

### First Time Setup
1. Launch the application
2. Click "Login Here" or "Login Using Browser" to authenticate with your Audible account
3. If using browser login, copy the error page URL after authentication and paste it into the application
4. Configure your library download path in Settings

### Downloading Books
1. Browse your library from the main view
2. Click "Download" on any available audiobook
3. Wait for download and decryption to complete
4. Access downloaded files using "Open Directory"

### Merging Multi-Part Books
1. After downloading a multi-part audiobook, click "Merge Parts"
2. Choose merge options:
   - Trim Audible messages between parts (recommended)
   - Delete parts when complete (optional)
3. Wait for merge operation to complete

## Configuration

Settings can be accessed from the settings icon in the top navigation bar:

- **Library Directory**: Choose where downloaded audiobooks are stored
- **Theme**: Toggle between light and dark mode
- **Account**: Logout and deregister device

## Technical Details

This application registers as a legitimate Audible device and uses official Audible APIs to access your library. All downloads are performed through authorized channels using your account credentials.

### File Organization
Downloaded audiobooks are organized in the following structure:
```
Library Path/
├── Author Name/
│   ├── Book Title/
│   │   ├── Book Title.m4a (merged file)
│   │   └── Book Title Part X.m4a (individual parts)
```

### Supported Audio Formats
The application downloads audiobooks in M4A format with AAC encoding. Codec selection is automatic based on availability, prioritizing higher quality options.

## License

This project is licensed under the GNU General Public License v3.0. See LICENSE.txt for details.

## Disclaimer

This application is intended for personal use with audiobooks you have legally purchased through Audible. Users are responsible for complying with Audible's Terms of Service and applicable copyright laws in their jurisdiction.

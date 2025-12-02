#!/usr/bin/env python3
"""
Simple HTTP Server for Sample SPA
Run this script to serve the application locally.
"""

import http.server
import socketserver
import os
import sys

PORT = 8080

class MyHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        # Add CORS headers for local development
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type, Authorization')
        super().end_headers()

    def do_OPTIONS(self):
        self.send_response(200)
        self.end_headers()

def main():
    # Change to the script's directory
    os.chdir(os.path.dirname(os.path.abspath(__file__)))

    with socketserver.TCPServer(("", PORT), MyHTTPRequestHandler) as httpd:
        print("=" * 60)
        print("Sample SPA Server")
        print("=" * 60)
        print(f"Server running at http://localhost:{PORT}/")
        print(f"\nOpen your browser and navigate to:")
        print(f"  http://localhost:{PORT}/index.html")
        print("\nPress Ctrl+C to stop the server")
        print("=" * 60)

        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\n\nServer stopped.")
            sys.exit(0)

if __name__ == "__main__":
    main()


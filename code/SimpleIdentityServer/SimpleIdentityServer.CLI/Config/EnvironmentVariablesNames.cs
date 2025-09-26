using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIdentityServer.CLI.Config
{
    public static class EnvironmentVariablesNames
    {
        /// <summary>
        /// Certificate password environment variable key.
        /// Used to specify the password for certificate files used by OpenIddict.
        /// </summary>
        public static readonly string CertificatePassword = "SIMPLE_IDENTITY_SERVER_CERT_PASSWORD";

        public static readonly string DefaultConnectionString = "SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING";
    }
}

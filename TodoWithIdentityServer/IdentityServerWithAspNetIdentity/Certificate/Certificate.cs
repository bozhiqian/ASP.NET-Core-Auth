using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServerWithAspNetIdentity.Certificate
{
    public class Certificate
    {
        public static X509Certificate2 Get(string thumbPrint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, true);
                if (certCollection.Count == 0)
                {
                    throw new Exception("The specified certificate wasn't found. Check the specified thumbprint.");
                }

                return certCollection[0];
            }
        }
    }
}

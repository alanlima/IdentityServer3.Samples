using SampleWCFApiHost.Config;
using System;
using System.Collections.Generic;
using System.IdentityModel.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Threading.Tasks;

namespace SampleWCFApiHost
{
    class Program
    {
        static void Main(string[] args)
        {
            //EndpointAddress addressHTTPS = new EndpointAddress("https://localhost:2728/Service1.svc");
            EndpointAddress addressHTTP = new EndpointAddress("http://localhost:2729/Service1.svc");

            using (var host = new ServiceHost(typeof(Service1), addressHTTP.Uri ) )
            {
                ConfigureForJWTToken(host, addressHTTP.Uri);

                //Adding metadata exchange endpoint
                Binding mexBinding = MetadataExchangeBindings.CreateMexHttpBinding();                
                host.AddServiceEndpoint(typeof(IMetadataExchange), mexBinding, "mex");                               

                host.Open();

                Console.WriteLine("The service is ready at {0}", addressHTTP.Uri);
                Console.WriteLine("Press [Enter] to exit...");
                Console.ReadLine();
                host.Close();
            }
        }
        
        private static void ConfigureForJWTToken(ServiceHost host, Uri address)
        {
            // Extract the ServiceCredentials behavior or create one.
            ServiceCredentials serviceCredentials = host.Description.Behaviors.Find<ServiceCredentials>();
            if (serviceCredentials == null)
            {
                serviceCredentials = new ServiceCredentials();
                host.Description.Behaviors.Add(serviceCredentials);
            }

            // Set the service certificate.
            host.Credentials.ServiceCertificate.Certificate = Certificate.Get();
            host.Credentials.UseIdentityConfiguration = true;

            IdentityConfiguration idConfiguration = new IdentityConfiguration();

            idConfiguration.SecurityTokenHandlers.Add(new CustomJwtSecurityTokenHandler.CustomJwtSecurityTokenHandler());            

            host.Credentials.IdentityConfiguration = idConfiguration;

            // Create the custom binding and add an endpoint to the service.
            Binding customTokenBinging = CreateBindingForJWTToken();
            host.AddServiceEndpoint(typeof(IService1), customTokenBinging, address);
        }

        static Binding CreateBindingForJWTToken()
        {
            HttpTransportBindingElement httpTransport = new HttpTransportBindingElement();

            TransportSecurityBindingElement messageSecurity = new TransportSecurityBindingElement();

            messageSecurity.AllowInsecureTransport = true;
            messageSecurity.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Default;
            messageSecurity.IncludeTimestamp = true;
            
            IssuedSecurityTokenParameters issuerTokenParameters = new IssuedSecurityTokenParameters();

            issuerTokenParameters.TokenType = "urn:ietf:params:oauth:token-type:jwt";

            messageSecurity.EndpointSupportingTokenParameters.Signed.Add(issuerTokenParameters);

            TextMessageEncodingBindingElement encodingElement = new TextMessageEncodingBindingElement(MessageVersion.Soap12, Encoding.UTF8);

            return new CustomBinding(messageSecurity, encodingElement, httpTransport);
        }
    }
}

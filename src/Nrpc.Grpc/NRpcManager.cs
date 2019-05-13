﻿namespace Nrpc.Grpc
{
    public static class NRpcManager
    {
        public static ServiceProxy CreateServiceProxy(string host, int port, params object[] instances)
        {
            return new ServiceProxy(host, port, instances);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(string host, int port, int timeoutInterval = 1200000)
        {
            var factory = new ClientConnectionFactory(host, port);
            return new GrpcClientProxy<TService>(factory, timeoutInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(ClientConnectionFactory factory, int timeoutInterval = 1200000)
        {
            return new GrpcClientProxy<TService>(factory, timeoutInterval);
        }
    }
}
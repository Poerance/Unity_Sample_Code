namespace Service_Locator {
  using System;
  using System.Collections.Generic;
  
  public static class ServiceLocator {
      public static Dictionary<Type, object> services = new Dictionary<Type, object>();
  
      public static void Register<T>(T service) where T : IService {
          if (services.ContainsKey(service.GetType())) {
              throw new Exception("Service already registered!");
          }
          services[service.GetType()] = service;
      }
  
      public static T Get<T>() where T : IService {
          if (!services.ContainsKey(typeof(T))) {
              throw new Exception("Service not registered!");
          }
          return (T)services[typeof(T)];
      }
  
      public static bool Exists<T>() where T : IService {
          return services.ContainsKey(typeof(T));
      }
  
      public static void Unregister<T>() where T : IService {
          if (!services.ContainsKey(typeof(T))) {
              throw new Exception("Service not registered!");
          }
          services.Remove(typeof(T));
      }
  }
}

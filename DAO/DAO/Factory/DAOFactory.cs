using System;
using System.Collections;
using System.Reflection;
using DAO.Config;
using DAO.DAO.InterfaceDAO;
using Entities.Logging;

namespace DAO.DAO.Factory
{
    /// <summary>
    /// Factory class to instantiate the correct DAO.
    /// </summary>
    public class DAOFactory
	{
		private static IDictionary hash = new Hashtable();
		
		/// <summary>
		/// Get the correct DAO object to use for the specified identifier.
		/// </summary>
		/// <param name="identifier">Identifier for the DAO.</param>
		/// <returns>The DAO to use.</returns>
		/// <exception cref="Exception">If using fakes and real DAO at the same time.</exception>
		/// <exception cref="Exception">No DAO with the specified identifier.</exception>
		/// <exception cref="Exception">Problems creating the DAO.</exception>
		public static IDAO GetDAO(string identifier)
		{		
			IDAO daoToUse;
			
			if(ContainsFakeForDAO(identifier))
			{
				return (IDAO)hash[identifier];	
			}
			if(hash.Count > 0)
			{
				// we want to be sure that we always run on fakes only or original dao's only
				// not a mix of them!
				throw new Exception("DAOFactory has fakes inserted and cannot use original and fakes at the same time. identifier="+identifier);
			}
			
			Type daoType = DAOConfiguration.GetDAOType(identifier);				

			if(daoType == null)
			{
				Exception dce = new Exception("Could not find the DAO to use for the identifier: " + identifier);
				
				throw dce;
			}

			try
			{
				ConstructorInfo cInfo = daoType.GetConstructor(Type.EmptyTypes);
				object obj = cInfo.Invoke(new object[0]);
				daoToUse = (IDAO) obj;
				
				return daoToUse;
			}
			catch (Exception ex)
			{
				Loggers.SVP.Exception(ex.Message, ex);
				Exception dce = new Exception("Could not create the DAO with the identifier: "+ identifier, ex);
				throw dce;
			}
		}

		/// <summary>
		/// Registers a fake DAO to be used in unit testing.
		/// 
		/// Fakes remain in the list until they are programmatically cleared. 
		/// Any test that registers a fake should take care to clear the fake
		/// at the end of the test, preferably in a catch block or in a test
		/// teardown method.
		/// 
		/// USE IN TESTING ONLY!
		/// </summary>
		/// <param name="identifier">The identifier of the DAO to fake.</param>
		/// <param name="dao">The fake DAO.</param>
		/// <exception cref="Exception">Trying to register for an allready registered fake.</exception>
		public static void RegisterFake(string identifier, IDAO dao)
		{
			if(ContainsFakeForDAO(identifier))
			{
				throw new Exception("Allready contains a fake for the identifier=" + identifier);
			}
			hash.Add(identifier, dao);
		}
		
		/// <summary>
		/// Clear all fake DAO from the Factory.
		/// 
		/// USE IN TESTING ONLY!
		/// </summary>
		public static void ClearFakes()
		{
			hash.Clear();
		}
		
		/// <summary>
		/// Does the Factory contain a fake for a specific DAO.
		/// 
		/// USE IN TESTING ONLY!
		/// </summary>
		/// <param name="identifier">The identifier for the fake DAO.</param>
		/// <returns>true if the factory contains a fake, else false.</returns>
		public static bool ContainsFakeForDAO(string identifier)
		{
			return hash.Contains(identifier);
		}
	}
}
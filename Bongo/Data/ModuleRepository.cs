using Bongo.Models;

namespace Bongo.Data
{
    public class ModuleRepository : IModuleRepository
    {
        private readonly AppDbContext _appDbContext;
        public ModuleRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public void Add(Module module)
        {
            _appDbContext.Modules.Add(module);
        }

        public void Delete(Module module)
        {
            _appDbContext.Modules.Remove(module);
        }

        public IEnumerable<Module> GetAllSessions()
        {
            return _appDbContext.Modules.ToList();
        }

        public void SaveChanges()
        {
            _appDbContext.SaveChanges();
        }
    }
    public class MockModuleRepository : IModuleRepository
    {
        private List<Module> lstModules;
        public MockModuleRepository()
        {
            AddMockModules();
        }
        public void Add(Module module)
        {
            if (lstModules == null)
                lstModules = new List<Module>();
            lstModules.Add(module);
        }

        public void Delete(Module module)
        {
            if (lstModules.Count > 0)
                lstModules.Remove(module);
        }

        public IEnumerable<Module> GetAllSessions()
        {
            if (lstModules == null)
                AddMockModules();
            return lstModules;
        }

        public void SaveChanges() { }
        private void AddMockModules()
        {
            Add(new Module { ModuleCode = "CSIS3734", ModuleName = "Internet Programming" });
            Add(new Module { ModuleCode = "CSIS3714", ModuleName = "Databases Part 2" });
            Add(new Module { ModuleCode = "MATM3734", ModuleName = "Discrete Mathematics" });
            Add(new Module { ModuleCode = "MATM3714", ModuleName = "Complex Analysis" });
            Add(new Module { ModuleCode = "EDED3712", ModuleName = "ePortfolio Development" });
            Add(new Module { ModuleCode = "BCIS3734", ModuleName = "Simple Shit" });
            Add(new Module { ModuleCode = "EALN1508", ModuleName = "Sekgowa Morena" });
        }
    }
}

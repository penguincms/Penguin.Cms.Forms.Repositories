using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Messaging.Persistence.Messages;
using Penguin.Persistence.Abstractions.Attributes.Rendering;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Reflection;
using Penguin.Security.Abstractions.Extensions;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Penguin.Cms.Forms.Repositories
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    public class FormRepository : AuditableEntityRepository<JsonForm>
    {
        protected ISecurityProvider<Form> SecurityProvider { get; set; }
        private const string FORM_NAME_COLLISION_MESSAGE = "Can not save form with name that matches display name of form class";

        public FormRepository(IPersistenceContext<JsonForm> dbContext, ISecurityProvider<Form> securityProvider = null, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
            SecurityProvider = securityProvider;
        }

        public override void AcceptMessage(Updating<JsonForm> update)
        {
            if (update is null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            Type ConcreteForm = GetConcreteFormType(update.Target.Name);

            if (ConcreteForm != null)
            {
                throw new Exception(FORM_NAME_COLLISION_MESSAGE);
            }

            base.AcceptMessage(update);
        }

        public override JsonForm Find(int Id) => SecurityProvider.TryFind(base.Find(Id));

        public Form GetByName(string Name)
        {
            if (Name is null)
            {
                throw new ArgumentNullException(nameof(Name));
            }

            Type ConcreteForm = GetConcreteFormType(Name);

            if (ConcreteForm is null)
            {
                return this.Where(j => j.ExternalId == Name).ToList().Where(f => SecurityProvider.TryCheckAccess(f)).SingleOrDefault();
            }
            else
            {
                if (Activator.CreateInstance(ConcreteForm) is Form toReturn)
                {
                    if (SecurityProvider.TryCheckAccess(toReturn))
                    {
                        return toReturn;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    throw new Exception("What the fuck?");
                }
            }
        }

        [SuppressMessage("Globalization", "CA1307:Specify StringComparison")]
        private static Type GetConcreteFormType(string Name)
        {
            if (Name is null)
            {
                throw new ArgumentNullException(nameof(Name));
            }

            string ExternalId = Name.Replace("-", " ");
            return TypeFactory.GetDerivedTypes(typeof(Form)).Where(t => (t.GetCustomAttribute<DisplayAttribute>()?.Name ?? t.Name) == ExternalId).FirstOrDefault();
        }
    }
}
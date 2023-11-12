using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZikmund.DataContracts.Email;

public class EmailRecipient
{
	public EmailRecipient(string name, string email) : this(email)
	{
		Name = name;
	}

	public EmailRecipient(string email)
	{
		Email = email;
	}

	public string Name { get; } = "";

	public string Email { get; } = "";
}

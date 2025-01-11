﻿using CSharpFunctionalExtensions;

namespace GamaEdtech.Back.Domain.Schools;

public class School : Entity<Guid>
{

	public SchoolName Name { get; private set; }
	public SchoolType Type { get; private set; }
	public Address Address { get; private set; }

	protected School() { }

	public School(SchoolName name, SchoolType type, Address address)
	{
		Name = name;
		Type = type;
		Address = address;
	}
}


public enum SchoolType
{
	Public,
	Private
}
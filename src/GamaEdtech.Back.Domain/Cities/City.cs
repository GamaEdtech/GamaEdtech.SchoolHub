﻿using GamaEdtech.Back.Domain.Base;

namespace GamaEdtech.Back.Domain.Cities;

public class City : AggregateRoot
{
	public string Name { get; private set; }
	public Id? StateId { get; private set; }
	public Id CountryId { get; private set; }

	public City(string name, Id? stateId, Id countryId)
	{
		Name = name;
		StateId = stateId;
		CountryId = countryId;
	}
}

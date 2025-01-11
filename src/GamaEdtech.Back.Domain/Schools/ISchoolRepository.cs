﻿namespace GamaEdtech.Back.Domain.Schools;

public interface ISchoolRepository
{
	public Task<IReadOnlyList<School>> Find(string? namePattern = null);
	public Task<IReadOnlyList<School>> FindByLocation(
		Location location, double radiusInKm = 0);

	public Task Add(School school);
}
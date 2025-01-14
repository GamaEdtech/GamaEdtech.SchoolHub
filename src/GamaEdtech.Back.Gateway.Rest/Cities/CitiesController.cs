﻿using Dapper;
using GamaEdtech.Back.DataSource.Utils;
using GamaEdtech.Back.Domain.Base;
using GamaEdtech.Back.Domain.Cities;
using GamaEdtech.Back.Domain.Countries;
using GamaEdtech.Back.Domain.States;
using GamaEdtech.Back.Gateway.Rest.Common;
using GamaEdtech.Back.Gateway.Rest.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace GamaEdtech.Back.Gateway.Rest.Cities;

[Route("api/[controller]")]
[ApiController]
public class CitiesController : ControllerBase
{
	private readonly ConnectionString _connectionString;
	private readonly GamaEdtechDbContext _dbContext;
	private readonly IStateRepository _stateRepository;
	private readonly ICountryRepository _countryRepository;
	private readonly ICityRepository _cityRepository;

	public CitiesController(
		ConnectionString connectionString,
		GamaEdtechDbContext dbContext, 
		IStateRepository stateRepository, 
		ICountryRepository countryRepository, 
		ICityRepository cityRepository)
	{
		_connectionString = connectionString;
		_dbContext = dbContext;
		_stateRepository = stateRepository;
		_countryRepository = countryRepository;
		_cityRepository = cityRepository;
	}

	[HttpGet]
	[PaginationTransformer]
	[SortingTransformer(DefaultSortKey = "Name", ValidSortKeys = "Name")]
	public async Task<IActionResult> List(
		[FromQuery] FilterCitiesDto filtering,
		[FromQuery] SortingDto sorting,
		[FromQuery] PaginationDto pagination)
	{

		var where = "";

		if(filtering.CountryId.HasValue)
		{
			where = "WHERE [c].[CountryId] = @CountryId ";

			if (filtering.StateId.HasValue)
				where += "AND [c].[StateId] = @StateId ";
		}else if (filtering.StateId.HasValue)
			where += "WHERE [c].[StateId] = @StateId ";


		var query = @"
			SELECT 
				[c].[Id], [c].[Name], 
				[co].[Name] AS Country,
				[s].[Name] AS State
			FROM [GamaEdtech].[dbo].[City] c
			JOIN [GamaEdtech].[dbo].[Country] co
				ON [c].[CountryId] = [co].[Id]
			LEFT JOIN [GamaEdtech].[dbo].[State] s
				ON [c].[StateId] = [s].[Id] " +
			where + 
			"ORDER BY [" + sorting.SortBy + "] " + sorting.Order + @"
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"; 

		using (var connection = new SqlConnection(_connectionString.Value))
		{
			var cities = await connection.QueryAsync<CityInListDto>(
				query,
				new
				{
					CountryId = filtering.CountryId,
					StateId = filtering.StateId,
					Offset = (pagination.Page - 1) * pagination.PageSize,
					PageSize = pagination.PageSize,
				});

			return Ok(Envelope.Ok(cities));
		}
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetDetail([FromRoute] int id)
	{
		var query = @"
            SELECT 
                [c].[Id], [c].[Name], 
                [co].[Id], [co].[Name], [co].[Code],
                [s].[Id], [s].[Name], [s].[Code]
            FROM [GamaEdtech].[dbo].[City] c
            JOIN [GamaEdtech].[dbo].[Country] co
                ON [c].[CountryId] = [co].[Id]
            LEFT JOIN [GamaEdtech].[dbo].[State] s
                ON [c].[StateId] = [s].[Id]
            WHERE [c].[Id] = @CityId";

		using (var connection = new SqlConnection(_connectionString.Value))
		{
			var city = (await connection
				.QueryAsync<CityDetailDto, CountryInCityDto, StateInCityDto?, CityDetailDto>(
					query,
					(city, country, state) =>
					{
						city.Country = country;
						city.State = state;
						return city;
					},
					new 
					{ 
						CityId = id 
					},
					splitOn: "Id,Id"))
				.FirstOrDefault();

			if (city is null)
				return NotFound();

			return Ok(Envelope.Ok(city));
		}
	}


	[HttpPost]
	public async Task<IActionResult> Add([FromBody] AddCityDto dto)
	{
		if(dto.StateId.HasValue)
		{
			var state = await _stateRepository.GetBy(new Id(dto.StateId.Value));
			if(state is null)
				return NotFound();

			if(await _cityRepository.ContainsCityWithNameInState(dto.Name, state.Id))
				return BadRequest(Envelope.Error("name is duplicate"));

			await _cityRepository.Add(state.CreateCityWith(dto.Name));
		}
		else
		{
			var country = await _countryRepository.GetBy(new Id(dto.CountryId));
			if (country is null)
				return NotFound();

			if (await _cityRepository.ContainsCityWithNameInCountry(dto.Name, country.Id))
				return BadRequest(Envelope.Error("name is duplicate"));

			await _cityRepository.Add(country.CreateCityWith(dto.Name));
		}

		await _dbContext.SaveChangesAsync();

		return Created();
	}

	[HttpPut("{id:int}/EditInfo")]
	public async Task<IActionResult> EditInfo(
		[FromRoute] int id, [FromBody] EditCityInfoDto dto)
	{
		var city = await _cityRepository.GetBy(new Id(id));

		if (city is null)
			return NotFound();

		if (dto.Name == city.Name)
			return NoContent();

		if (city.IsPartOfAState && await _cityRepository.ContainsCityWithNameInState(dto.Name, city.CountryId!))
			return BadRequest(Envelope.Error("name is duplicate"));

		if (await _cityRepository.ContainsCityWithNameInCountry(dto.Name, city.CountryId))
			return BadRequest(Envelope.Error("name is duplicate"));

		city.EditInfo(dto.Name);

		await _dbContext.SaveChangesAsync();

		return NoContent();
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Remove([FromRoute] int id)
	{
		var city = await _cityRepository.GetBy(new Id(id));

		if (city is null)
			return NotFound();

		await _cityRepository.Remove(city);
		await _dbContext.SaveChangesAsync();

		return NoContent();
	}
}

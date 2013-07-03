﻿#region BSD Licence
/* Copyright (c) 2013, Doxense SARL
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Layers.Tables.Tests
{
	using FoundationDB.Client;
	using FoundationDB.Client.Tests;
	using FoundationDB.Layers.Tuples;
	using FoundationDB.Linq;
	using NUnit.Framework;
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using System.Linq;
	using FoundationDB.Layers.Indexing;

	[TestFixture]
	public class IndexingFacts
	{

		[Test]
		public async Task Test_Can_Combine_Indexes()
		{

			using (var db = await TestHelpers.OpenTestDatabaseAsync())
			{

				var location = db.Partition("Indexing");

								// clear previous values
				await TestHelpers.DeleteSubspace(db, location);

				// summon our main cast
				var characters = new List<Character>()
				{
					new Character { Id = 1, Name = "Super Man", Brand="DC", HasSuperPowers = true, IsVilain = false },
					new Character { Id = 2, Name = "Batman", Brand="DC", IsVilain = false },
					new Character { Id = 3, Name = "Joker", Brand="DC", IsVilain = true },
					new Character { Id = 4, Name = "Iron Man", Brand="Marvel", IsVilain = false },
					new Character { Id = 5, Name = "Magneto", Brand="Marvel", HasSuperPowers = true, IsVilain = true },
					new Character { Id = 6, Name = "Catwoman", Brand="DC", IsVilain = default(bool?) },
				};

				var indexBrand = new FdbSimpleIndex<long, string>(location.Partition("CharactersByBrand"));
				var indexSuperHero = new FdbSimpleIndex<long, bool>(location.Partition("SuperHeros"));
				var indexAlignment = new FdbSimpleIndex<long, bool?>(location.Partition("FriendsOrFoe"));

				// index everything
				using(var tr = db.BeginTransaction())
				{
					foreach(var character in characters)
					{
						indexBrand.Add(tr, character.Id, character.Brand);
						indexSuperHero.Add(tr, character.Id, character.HasSuperPowers);
						indexAlignment.Add(tr, character.Id, character.IsVilain);
					}
					await tr.CommitAsync();
				}

#if DEBUG
				await TestHelpers.DumpSubspace(db, location);
#endif

				// super heros only (sorry Batman!)
				using (var tr = db.BeginTransaction())
				{
					var superHeros = await indexSuperHero.LookupAsync(tr, value: true);
					Assert.That(superHeros, Is.EqualTo(characters.Where(c => c.HasSuperPowers).Select(c => c.Id).ToList()));
				}

				// Versus !
				using (var tr = db.BeginTransaction())
				{
					var dc = await indexBrand.LookupAsync(tr, value: "DC");
					Assert.That(dc, Is.EqualTo(characters.Where(c => c.Brand == "DC").Select(c => c.Id).ToList()));

					var marvel = await indexBrand.LookupAsync(tr, value: "Marvel");
					Assert.That(marvel, Is.EqualTo(characters.Where(c => c.Brand == "Marvel").Select(c => c.Id).ToList()));
				}

				// Vilains with superpowers are the worst
				using (var tr = db.BeginTransaction())
				{
					var first = indexAlignment.Lookup(tr, value: true);
					var second = indexSuperHero.Lookup(tr, value: true);

					var merged = await first
						.Intersect(second)
						.ToListAsync();

					Assert.That(merged.Count, Is.EqualTo(1));
					Assert.That(merged[0] == characters.Single(c => c.Name == "Magneto").Id);
				}
			}

		}

		private sealed class Character
		{
			public long Id { get; set; }

			public string Name { get; set; }

			public string Brand { get; set; }

			public bool HasSuperPowers { get; set; }

			public bool? IsVilain { get; set; }
		}

	}

}
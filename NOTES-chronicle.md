# Chronicle pipeline: work notes

Done: daemons /secure/{events,chronicle,renown,rumors}.cs, catalogue /events/*.cs (+ places.cs), /std/chronicle_ledger.cs.
Next: tests/fixtures/chron_harness.cs, tests/chronicle_test.py, docs/CHRONICLE_IMPL.md.
Key fact: master.cs only lets wizlist/vault/players save, so the daemons cannot persist until master.cs gets the lines in the doc; the test patches its own copy of master.cs.

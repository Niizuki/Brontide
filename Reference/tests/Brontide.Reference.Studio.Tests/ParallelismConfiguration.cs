// Every test in this assembly owns what it touches: a temporary directory named by a fresh
// identifier, the provider processes it starts and stops, and the fakes it constructs. No fixture
// holds state one test writes and another reads, so the tests run together. The process category is
// where the wall clock went -- each of its tests spawns and waits on provider processes -- and run
// one at a time it was the floor under the whole repository gate.
//
// Four workers rather than NUnit's default of one per processor. A provider conversation carries the
// portable contract's ten-second limit on one blocking read of the process seam, and on a machine
// with twenty-two processors the default put over forty freshly built providers through start-up at
// once, where a cold one's first reply crossed that limit and an activation that had succeeded read
// as no longer serving. Four keeps the spawning bounded on any machine the gate runs on, including a
// four-processor continuous-integration runner already running this assembly beside seven others.
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(4)]

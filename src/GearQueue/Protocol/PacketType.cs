namespace GearQueue.Protocol;

internal enum PacketType
{
	CanDo = 1,					// REQ	Worker
	CantDo = 2,					// REQ	Worker
	ResetAbilities = 3,			// REQ	Worker
	PreSleep = 4,				// REQ	Worker
	Noop = 6,					// RES	Worker
	SubmitJob = 7,				// REQ	Client
	JobCreated = 8,				// RES	Client
	GrabJob = 9,				// REQ	Worker
	NoJob = 10,					// RES	Worker
	JobAssign = 11,				// RES	Worker
	WorkStatus = 12,			// REQ	Worker
	WorkComplete = 13,			// REQ	Worker
	WorkFail = 14,				// REQ	Worker
	GetStatus = 15,				// REQ	Client
	EchoReq = 16,				// REQ	Client/Worker
	EchoRes = 17,				// RES	Client/Worker
	SubmitJobBg = 18,			// REQ	Client
	Error = 19,					// RES	Client/Worker
	StatusRes = 20,				// RES	Client
	SubmitJobHigh = 21,			// REQ	Client
	SetClientId = 22,			// REQ	Worker
	CanDoTimeout = 23,			// REQ	Worker
	AllYours = 24,				// REQ	Worker
	WorkException = 25,			// REQ	Worker
	OptionReq = 26,				// REQ	Client/Worker
	OptionRes = 27,				// RES	Client/Worker
	WorkData = 28,				// REQ	Worker
	WorkWarning = 29,			// REQ	Worker
	GrabJobUniq = 30,			// REQ	Worker
	JobAssignUniq = 31,			// RES	Worker
	SubmitJobHighBg = 32,		// REQ	Client
	SubmitJobLow = 33,			// REQ	Client
	SubmitJobLowBg = 34,		// REQ	Client
	SubmitJobSched = 35,		// REQ	Client
	SubmitJobEpoch = 36,		// REQ	Client
	SubmitReduceJob = 37,		// REQ	Client
	SubmitReduceJobBg = 38,		// REQ	Client
	GrabJobAll = 39,			// REQ	Worker
	JobAssignAll = 40,			// RES	Worker
	GetStatusUnique = 41,		// REQ	Client
	StatusResUnique = 42,		// RES	Client
}
export interface CreateUserInvitationRequest {
	email: string;
	roleName: "Bettor" | "Admin";
}

export interface CreateUserInvitationResult {
	id: number;
	email: string;
	roleName: "Bettor" | "Admin";
	wasAlreadyInvited: boolean;
}

export interface AuditReportMetadata {
	label: string;
	description: string;
	isDerivedFromCurrentState: boolean;
}

export interface AuditBalanceSummary {
	metadata: AuditReportMetadata;
	rows: ReadonlyArray<AuditBalanceSummaryRow>;
}

export interface AuditBalanceSummaryRow {
	userId: number;
	displayName: string;
	email: string;
	availableBalanceCc: number;
	pendingTotalCc: number;
	derivedTotalBalanceCc: number;
	wonTotalCc: number;
	lostTotalCc: number;
	rescueDebtCc: number;
	rescueCount: number;
}

export interface AuditUserSubledger {
	metadata: AuditReportMetadata;
	user: AuditBalanceSummaryRow;
	items: ReadonlyArray<AuditLedgerItem>;
}

export interface AuditLedgerItem {
	sourceType: string;
	sourceId: number;
	label: string;
	placedAtUtc: string;
	stakeAmountCc: number;
	status: string;
	creditAmountCc: number;
	lossAmountCc: number;
	pendingAmountCc: number;
	pendingReason: string | null;
	metadata: ReadonlyArray<AuditLedgerMetadataItem>;
}

export interface AuditLedgerMetadataItem {
	label: string;
	value: string;
}

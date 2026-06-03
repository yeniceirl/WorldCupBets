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

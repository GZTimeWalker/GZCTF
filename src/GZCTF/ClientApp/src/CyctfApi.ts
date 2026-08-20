import { Api } from '@Api'

// Re-export the main API instance
const cyctfApi = new Api()

export default cyctfApi

// Re-export types from Api.ts for convenience
export type {
  AwardModel,
  AwardRequest,
  DivisionExtensionModel,
  DivisionExtensionRequest,
  GameExtensionModel,
  GameExtensionRequest,
  RegistrationModel,
  RegistrationRequest,
  RegistrationReviewRequest,
  SponsorModel,
  SponsorRequest,
} from '@Api'

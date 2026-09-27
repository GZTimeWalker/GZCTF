import { useParams } from 'react-router'
import { Center, Loader, Text } from '@mantine/core'
import { ChallengeWorkspace } from '@Components/learning/ChallengeWorkspace'
import { WithNavBar } from '@Components/WithNavbar'

const StandaloneChallenge = () => {
  const { id } = useParams()
  return <WithNavBar minWidth={0}>{id ? <ChallengeWorkspace challengeId={id} /> : <Center><Loader /></Center>}</WithNavBar>
}

export default StandaloneChallenge

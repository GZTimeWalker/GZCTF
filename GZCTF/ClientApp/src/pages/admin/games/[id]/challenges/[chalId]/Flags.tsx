import { FC} from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Stack,
} from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../../../../../Api'

import WithGameTab from '../../../../../../components/admin/WithGameTab'

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  return (
    <WithGameTab
      isLoading={!challenge}
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate(`/admin/games/${numId}/challenges`)}
        >
          返回上级
        </Button>
      }
    >
      <Stack>

      </Stack>
    </WithGameTab>
  )
}

export default GameChallengeEdit

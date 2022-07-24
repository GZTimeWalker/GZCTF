import { FC, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Card, Group } from '@mantine/core'
import { mdiBackburger, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../../../../Api'
import ChallengeCreateModal from '../../../../../components/admin/ChallengeCreateModal'
import WithGameTab from '../../../../../components/admin/WithGameTab'

const GameChallengeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const navigate = useNavigate()
  const [createOpened, setCreateOpened] = useState(false)

  const {
    data: challenges,
    error,
    mutate,
  } = api.edit.useEditGetGameChallenges(numId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  return (
    <WithGameTab
      headProps={{ position: 'left' }}
      isLoading={!challenges}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate('/admin/games')}
        >
          返回上级
        </Button>
      }
    >
      <Group position="center">
        <Button leftIcon={<Icon path={mdiPlus} size={1} />} onClick={() => setCreateOpened(true)}>
          新建题目
        </Button>
      </Group>
      {challenges &&
        !error &&
        challenges.map((challenge) => <Card key={challenge.id}>{challenge.title}</Card>)}
      <ChallengeCreateModal
        title="新建题目"
        centered
        size="30%"
        opened={createOpened}
        onClose={() => setCreateOpened(false)}
        onAddChallenge={(challenge) => mutate([challenge, ...(challenges ?? [])])}
      />
    </WithGameTab>
  )
}

export default GameChallengeEdit

import { FC } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Group, Stack, TextInput } from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeType } from '../../../../../../Api'
import WithGameTab from '../../../../../../components/admin/WithGameTab'

const OneAttachmentWithFlags: FC = () => {
  // const { id, chalId } = useParams()
  // const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  // const { data: challenge, mutate } = api.edit.useEditGetGameChallenge(numId, numCId, {
  //   refreshInterval: 0,
  //   revalidateIfStale: false,
  //   revalidateOnFocus: false,
  // })

  return (
    <Stack>
      <Group position="apart">
        <TextInput label="当前附件" />
      </Group>
    </Stack>
  )
}

const FlagsWithAttachments: FC = () => {
  return (
    <Stack>
      <Group position="apart">
        <Stack spacing="xs"></Stack>
      </Group>
    </Stack>
  )
}

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
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
            onClick={() => navigate(`/admin/games/${id}/challenges`)}
          >
            返回上级
          </Button>
          <Group position="right">
            <Button onClick={() => navigate(`/admin/games/${id}/challenges/${numCId}`)}>
              编辑题目信息
            </Button>
          </Group>
        </>
      }
    >
      {challenge && challenge.type === ChallengeType.DynamicAttachment ? (
        <FlagsWithAttachments />
      ) : (
        <OneAttachmentWithFlags />
      )}
    </WithGameTab>
  )
}

export default GameChallengeEdit

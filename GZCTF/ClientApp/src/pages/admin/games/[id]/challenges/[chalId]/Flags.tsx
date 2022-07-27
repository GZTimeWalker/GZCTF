import { FC, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Grid, Group, Radio, Stack, TextInput } from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeType, FileType } from '../../../../../../Api'
import WithGameTab from '../../../../../../components/admin/WithGameTab'

const FileTypeDesrcMap = new Map<FileType, string>([
  [FileType.None, '无附件'],
  [FileType.Remote, '远程文件'],
  [FileType.Local, '平台附件'],
])

// with only one attachment
const OneAttachmentWithFlags: FC = () => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const attachment = challenge?.flags.at(0)
  const [fileType, setFileType] = useState(attachment?.type ?? FileType.None)

  return (
    <Stack>
      <Grid>
        <Grid.Col span={4}>
          <Radio.Group
            required
            label="附件类型"
            value={fileType}
            onChange={(v) => setFileType(v as FileType)}
          >
            {Object.entries(FileType).map((type) => (
              <Radio key={type[0]} value={type[1]} label={FileTypeDesrcMap.get(type[1])} />
            ))}
          </Radio.Group>
        </Grid.Col>
        <Grid.Col span={8}>
          <TextInput label="附件链接" value={attachment?.url ?? ''} />
        </Grid.Col>
      </Grid>
      <Group position="right">

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

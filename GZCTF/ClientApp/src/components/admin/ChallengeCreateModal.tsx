import { FC, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Group, Modal, ModalProps, Select, Stack, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeInfoModel, ChallengeTag, ChallengeType } from '../../Api'

interface ChallengeCreateModalProps extends ModalProps {
  onAddChallenge: (game: ChallengeInfoModel) => void
}

export const ChallengeTypeLabelMap = new Map<ChallengeType, string>([
  [ChallengeType.StaticAttachment, '静态附件题'],
  [ChallengeType.StaticContainer, '静态容器题'],
  [ChallengeType.DynamicAttachment, '动态附件题'],
  [ChallengeType.DynamicContainer, '动态容器题'],
])

const ChallengeCreateModal: FC<ChallengeCreateModalProps> = (props) => {
  const { id } = useParams()
  const { onAddChallenge, ...modalProps } = props
  const [disabled, setDisabled] = useState(false)
  const navigate = useNavigate()
  const [challenge, setChallenge] = useInputState<ChallengeInfoModel>({
    title: '',
  })

  const onCreate = () => {
    if (challenge && challenge.title) {
      setDisabled(true)
      const numId = parseInt(id ?? '-1')
      api.edit
        .editAddGameChallenge(numId, challenge)
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '添加比赛题目成功',
            icon: <Icon path={mdiCheck} size={1} />,
          })
          onAddChallenge(data.data)
          navigate('/admin/games')
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
          setDisabled(false)
        })
    }
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <TextInput
          label="题目标题"
          type="text"
          required
          placeholder="Title"
          style={{ width: '100%' }}
          value={challenge.title}
          onChange={(e) => setChallenge({ ...challenge, title: e.target.value })}
        />

        <Group grow position="apart">
          <Select
            required
            label="题目类型"
            placeholder="Type"
            data={Object.entries(ChallengeType).map((type) => ({
              value: type[1],
              label: ChallengeTypeLabelMap.get(type[1]),
            }))}
          />
          <Select
            required
            label="题目标签"
            placeholder="Tag"
            data={Object.entries(ChallengeTag).map((tag) => ({
              value: tag[1],
              label: tag[0],
            }))}
          />
        </Group>

        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            创建题目
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default ChallengeCreateModal

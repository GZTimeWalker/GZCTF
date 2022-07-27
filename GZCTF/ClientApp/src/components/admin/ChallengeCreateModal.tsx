import { FC, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Modal, ModalProps, Select, Stack, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeInfoModel, ChallengeTag, ChallengeType } from '../../Api'
import {
  ChallengeTagItem,
  ChallengeTagLabelMap,
  ChallengeTypeItem,
  ChallengeTypeLabelMap,
} from '../ChallengeItem'

interface ChallengeCreateModalProps extends ModalProps {
  onAddChallenge: (game: ChallengeInfoModel) => void
}

const ChallengeCreateModal: FC<ChallengeCreateModalProps> = (props) => {
  const { id } = useParams()
  const { onAddChallenge, ...modalProps } = props
  const [disabled, setDisabled] = useState(false)
  const navigate = useNavigate()

  const [title, setTitle] = useInputState('')
  const [tag, setTag] = useState<string | null>(null)
  const [type, setType] = useState<string | null>(null)

  const onCreate = () => {
    if (title && tag && type) {
      console.log(title, tag, type)
      setDisabled(true)
      const numId = parseInt(id ?? '-1')

      api.edit
        .editAddGameChallenge(numId, {
          title: title,
          tag: tag as ChallengeTag,
          type: type as ChallengeType,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '添加比赛题目成功',
            icon: <Icon path={mdiCheck} size={1} />,
          })
          onAddChallenge(data.data)
          navigate(`/admin/games/${id}/challenges/${data.data.id}`)
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
          value={title}
          onChange={setTitle}
        />
        <Select
          required
          label="题目类型"
          description="创建后不可更改"
          placeholder="Type"
          value={type}
          onChange={setType}
          itemComponent={ChallengeTypeItem}
          data={Object.entries(ChallengeType).map((type) => {
            const data = ChallengeTypeLabelMap.get(type[1])
            return { value: type[1], ...data }
          })}
        />
        <Select
          required
          label="题目标签"
          placeholder="Tag"
          value={tag}
          onChange={setTag}
          itemComponent={ChallengeTagItem}
          data={Object.entries(ChallengeTag).map((tag) => {
            const data = ChallengeTagLabelMap.get(tag[1])
            return { value: tag[1], ...data }
          })}
        />
        <Button fullWidth disabled={disabled} onClick={onCreate}>
          创建题目
        </Button>
      </Stack>
    </Modal>
  )
}

export default ChallengeCreateModal

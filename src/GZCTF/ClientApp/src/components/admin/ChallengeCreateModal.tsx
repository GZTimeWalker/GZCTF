import { Button, ComboboxItem, Modal, ModalProps, Select, Stack, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { showErrorNotification } from '@Utils/ApiHelper'
import {
  ChallengeTagItem,
  ChallengeTypeItem,
  useChallengeTagLabelMap,
  useChallengeTypeLabelMap,
} from '@Utils/Shared'
import api, { ChallengeInfoModel, ChallengeTag, ChallengeType } from '@Api'

interface ChallengeCreateModalProps extends ModalProps {
  onAddChallenge: (game: ChallengeInfoModel) => void
}

const ChallengeCreateModal: FC<ChallengeCreateModalProps> = (props) => {
  const { id } = useParams()
  const { onAddChallenge, ...modalProps } = props
  const [disabled, setDisabled] = useState(false)
  const navigate = useNavigate()
  const challengeTagLabelMap = useChallengeTagLabelMap()
  const challengeTypeLabelMap = useChallengeTypeLabelMap()

  const [title, setTitle] = useInputState('')
  const [tag, setTag] = useState<string | null>(null)
  const [type, setType] = useState<string | null>(null)

  const { t } = useTranslation()

  const onCreate = () => {
    if (!title || !tag || !type) return

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
          message: t('admin.notification.games.challenges.created'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        onAddChallenge(data.data)
        navigate(`/admin/games/${id}/challenges/${data.data.id}`)
      })
      .catch((err) => {
        showErrorNotification(err, t)
        setDisabled(false)
      })
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <TextInput
          label={t('admin.content.games.challenges.title')}
          type="text"
          required
          placeholder="Title"
          value={title}
          onChange={setTitle}
        />
        <Select
          required
          label={t('admin.content.games.challenges.tag')}
          placeholder="Tag"
          value={tag}
          onChange={setTag}
          renderOption={ChallengeTagItem}
          data={Object.entries(ChallengeTag).map((tag) => {
            const data = challengeTagLabelMap.get(tag[1])
            return { value: tag[1], label: data?.name, ...data } as ComboboxItem
          })}
        />
        <Select
          required
          label={t('admin.content.games.challenges.type.label')}
          description={t('admin.content.games.challenges.type.description')}
          placeholder="Type"
          value={type}
          onChange={setType}
          renderOption={ChallengeTypeItem}
          data={Object.entries(ChallengeType).map((type) => {
            const data = challengeTypeLabelMap.get(type[1])
            return { value: type[1], label: data?.name, ...data } as ComboboxItem
          })}
        />
        <Button fullWidth disabled={disabled} onClick={onCreate}>
          {t('admin.button.challenges.new')}
        </Button>
      </Stack>
    </Modal>
  )
}

export default ChallengeCreateModal

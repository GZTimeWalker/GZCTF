import { Button, Group, Modal, ModalProps, Stack, Text, Textarea } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useEditChallenge } from '@Utils/useEdit'
import api from '@Api'

const FlagCreateModal: FC<ModalProps> = (props) => {
  const [disabled, setDisabled] = useState(false)

  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const [flags, setFlags] = useInputState('')

  const { challenge, mutate } = useEditChallenge(numId, numCId)

  const { t } = useTranslation()

  const onCreate = () => {
    if (!flags) {
      return
    }

    const flagList = flags
      .split('\n')
      .filter((x) => x.trim().length > 0)
      .map((x) => ({ flag: x }))

    setDisabled(true)
    api.edit
      .editAddFlags(numId, numCId, flagList)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.flag.created'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        challenge &&
          mutate({
            ...challenge,
            flags: [...(challenge.flags ?? []), ...flagList],
          })
      })
      .catch((err) => {
        showErrorNotification(err, t)
        setDisabled(false)
      })
      .finally(() => {
        setFlags('')
        setDisabled(false)
        props.onClose()
      })
  }

  return (
    <Modal {...props}>
      <Stack>
        <Text>
          <Trans i18nKey="admin.content.games.challenges.flag.create" />
        </Text>
        <Textarea
          value={flags}
          w="100%"
          disabled={disabled}
          autosize
          minRows={8}
          maxRows={8}
          onChange={setFlags}
          wrapperProps={{
            __vars: {
              '--input-font-family': 'var(--mantine-font-family-monospace)',
            },
          }}
        />
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            {t('admin.button.challenges.flag.add.normal')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default FlagCreateModal

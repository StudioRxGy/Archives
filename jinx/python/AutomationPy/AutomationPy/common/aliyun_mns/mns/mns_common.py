class RequestInfo:
    def __init__(self, request_id = None):
        """ this information will be send to MNS Server
            @note:
            :: request_id: used to search logs of this request
        """
        self.request_id = request_id

class TopicHelper:

    @staticmethod
    def generate_queue_endpoint(region, accountid, queue_name):
        """
            @type region: string
            @param region: the region of queue, such as: cn-hangzhou

            @type accountid: string
            @param accountid: the accountid of queue's owner

            @type queue_name: string
            @param queue_name
        """
        return "acs:mns:%s:%s:queues/%s" % (region, accountid, queue_name)

    @staticmethod
    def generate_mail_endpoint(mail_address):
        """
            @type mail_address: string
            @param mail_address: the address of mail
        """
        return "mail:directmail:%s" % mail_address

    @staticmethod
    def generate_sms_endpoint(phone=None):
        """
            @type phone: string
            @param phone: the number of phone
        """
        endpoint = "sms:directsms:anonymous" if phone is None else "sms:directsms:%s" % phone
        return endpoint
